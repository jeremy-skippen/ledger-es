using FluentValidation;

using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Ledgers;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Js.LedgerEs.Tests.Ledgers;

public sealed class CloseLedgerTest
{
    private readonly IServiceProvider _services;
    private readonly Mock<IEventClient> _mockEventClient;

    public CloseLedgerTest()
    {
        var serviceCollection = new ServiceCollection();

        _mockEventClient = new Mock<IEventClient>();

        serviceCollection
            .AddLogging()
            .AddAutoMapper(am =>
            {
                am.AddProfile<MappingProfile>();
            })
            .AddTransient<IValidator<CloseLedger>, CloseLedgerValidator>()
            .AddTransient<IRequestHandler<CloseLedger, LedgerClosed>, CloseLedgerHandler>()
            .AddSingleton(_mockEventClient.Object)
            .AddMediatRForUnitTests();

        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_RequestIsInvalid()
    {
        // Prepare
        var request = new CloseLedger(Guid.Empty);

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("LedgerId");
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_LedgerDoesNotExist()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var request = new CloseLedger(ledgerId);

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as LedgerWriteModel);

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("", "Cannot close a ledger that is not open");
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_LedgerIsAlreadyClosed()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new CloseLedger(ledgerId);

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);

        ledger.Open(new LedgerOpened(ledgerId, "Test Ledger"));
        ledger.Close(new LedgerClosed(ledgerId));

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("", "Cannot close a ledger that is not open");
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_LedgerHasBalance()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new CloseLedger(ledgerId);

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);

        ledger.Open(new LedgerOpened(ledgerId, "Test Ledger"));
        ledger.JournalReceipt(new ReceiptJournalled(ledgerId, "Test receipt", 1.00m));

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("", "Cannot close a ledger that has balance");
    }

    [Fact]
    public async Task Should_Succeed_When_LedgerIsOpenAndHasNoJournalEntries()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new CloseLedger(ledgerId);

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);

        ledger.Open(new LedgerOpened(ledgerId, "Test Ledger"));

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var response = await mediator.Send(request);

        // Assert
        Assert.Equal(request.LedgerId, response.LedgerId);

        _mockEventClient
            .Verify(e => e.AppendToStreamAsync(
                ledgerStreamName,
                It.IsAny<IWriteModel>(),
                ledger.Version - 2,
                response,
                It.IsAny<CancellationToken>()
            ), Times.Once);
    }

    [Fact]
    public async Task Should_Succeed_When_LedgerIsOpenAndHasZeroBalance()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new CloseLedger(ledgerId);

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);

        ledger.Open(new LedgerOpened(ledgerId, "Test Ledger"));
        ledger.JournalReceipt(new ReceiptJournalled(ledgerId, "Test receipt", 1.00m));
        ledger.JournalReceipt(new ReceiptJournalled(ledgerId, "Test receipt 2", 1.00m));
        ledger.JournalPayment(new PaymentJournalled(ledgerId, "Test payment", 2.00m));

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var response = await mediator.Send(request);

        // Assert
        Assert.Equal(request.LedgerId, response.LedgerId);

        _mockEventClient
            .Verify(e => e.AppendToStreamAsync(
                ledgerStreamName,
                It.IsAny<IWriteModel>(),
                ledger.Version - 2,
                response,
                It.IsAny<CancellationToken>()
            ), Times.Once);
    }
}
