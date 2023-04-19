using FluentValidation;

using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Ledgers;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Js.LedgerEs.Tests.Ledgers;

public sealed class JournalReceiptTest
{
    private readonly IServiceProvider _services;
    private readonly Mock<IEventClient> _mockEventClient;

    public JournalReceiptTest()
    {
        var serviceCollection = new ServiceCollection();

        _mockEventClient = new Mock<IEventClient>();

        serviceCollection
            .AddLogging()
            .AddAutoMapper(am =>
            {
                am.AddProfile<LedgerCommandToSerializableEventMappingProfile>();
            })
            .AddTransient<IValidator<JournalReceipt>, JournalReceiptValidator>()
            .AddTransient<IRequestHandler<JournalReceipt, ReceiptJournalled>, JournalReceiptHandler>()
            .AddSingleton(_mockEventClient.Object)
            .AddMediatRForUnitTests();

        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_RequestIsInvalid()
    {
        // Prepare
        var request = new JournalReceipt(
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
    public async Task Should_ThrowValidationException_When_LedgerIsClosed()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new JournalReceipt(
            LedgerId: ledgerId,
            Description: "Test Receipt",
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
        ex.ShouldHaveValidationErrorFor("", "Cannot receipt to a closed ledger");
    }

    [Fact]
    public async Task Should_Succeed_When_LedgerIsOpen()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new JournalReceipt(
            LedgerId: ledgerId,
            Description: "Test Receipt",
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
