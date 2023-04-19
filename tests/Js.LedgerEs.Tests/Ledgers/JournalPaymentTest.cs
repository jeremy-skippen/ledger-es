using FluentValidation;

using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Ledgers;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Js.LedgerEs.Tests.Ledgers;

public sealed class JournalPaymentTest
{
    private readonly IServiceProvider _services;
    private readonly Mock<IEventClient> _mockEventClient;

    public JournalPaymentTest()
    {
        var serviceCollection = new ServiceCollection();

        _mockEventClient = new Mock<IEventClient>();

        serviceCollection
            .AddLogging()
            .AddAutoMapper(am =>
            {
                am.AddProfile<LedgerCommandToSerializableEventMappingProfile>();
            })
            .AddTransient<IValidator<JournalPayment>, JournalPaymentValidator>()
            .AddTransient<IRequestHandler<JournalPayment, PaymentJournalled>, JournalPaymentHandler>()
            .AddSingleton(_mockEventClient.Object)
            .AddMediatRForUnitTests();

        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_RequestIsInvalid()
    {
        // Prepare
        var request = new JournalPayment(
            LedgerId: Guid.Empty,
            Description: "",
            Amount: -37m
        );

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("LedgerId");
        ex.ShouldHaveValidationErrorFor("Description");
        ex.ShouldHaveValidationErrorFor("Amount");
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_LedgerHasZeroBalance()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new JournalPayment(
            LedgerId: ledgerId,
            Description: "Test Payment",
            Amount: 37m
        );

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);

        ledger.Open(new LedgerOpened(ledgerId, "Test Ledger"));

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("Amount", "Ledger has insufficient balance - $0.00");
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_LedgerIsClosed()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new JournalPayment(
            LedgerId: ledgerId,
            Description: "Test Payment",
            Amount: 37m
        );

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("", "Cannot pay from a closed ledger");
    }

    [Fact]
    public async Task Should_Succeed_When_LedgerHasBalance()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new JournalPayment(
            LedgerId: ledgerId,
            Description: "Test Payment",
            Amount: 37m
        );

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ledger);

        ledger.Open(new LedgerOpened(ledgerId, "Test Ledger"));
        ledger.JournalReceipt(new ReceiptJournalled(ledgerId, "Test receipt", 125.00m));

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var response = await mediator.Send(request);

        // Assert
        Assert.Equal(request.LedgerId, response.LedgerId);
        Assert.Equal(request.Description, response.Description);
        Assert.Equal(request.Amount, response.Amount);

        _mockEventClient
            .Verify(e => e.AppendToStreamAsync(
                ledgerStreamName,
                It.IsAny<IWriteModel>(),
                ledger.Version - 1,
                response,
                It.IsAny<CancellationToken>()
            ), Times.Once);
    }
}
