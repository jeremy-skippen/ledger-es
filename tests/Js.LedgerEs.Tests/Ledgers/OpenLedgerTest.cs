using EventStore.Client;

using FluentValidation;

using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Ledgers;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Js.LedgerEs.Tests.Ledgers;

public sealed class OpenLedgerTest
{
    private readonly IServiceProvider _services;
    private readonly Mock<IEventClient> _mockEventClient;

    public OpenLedgerTest()
    {
        var serviceCollection = new ServiceCollection();

        _mockEventClient = new Mock<IEventClient>();

        serviceCollection
            .AddLogging()
            .AddAutoMapper(am =>
            {
                am.AddProfile<MappingProfile>();
            })
            .AddTransient<IValidator<OpenLedger>, OpenLedgerValidator>()
            .AddTransient<IRequestHandler<OpenLedger, LedgerOpened>, OpenLedgerHandler>()
            .AddSingleton(_mockEventClient.Object)
            .AddMediatRForUnitTests();

        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_RequestIsInvalid()
    {
        // Prepare
        var request = new OpenLedger(Guid.Empty, "");

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(request));

        // Assert
        ex.ShouldHaveValidationErrorFor("LedgerId");
        ex.ShouldHaveValidationErrorFor("LedgerName");
    }

    [Fact]
    public async Task Should_ThrowValidationException_When_LedgerIsAlreadyOpen()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new OpenLedger(ledgerId, "Test Ledger");

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
        ex.ShouldHaveValidationErrorFor("", "Cannot open a ledger that is already opened");
    }

    [Fact]
    public async Task Should_Succeed_When_LedgerDoesNotExist()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var request = new OpenLedger(ledgerId, "Test Ledger");

        _mockEventClient
            .Setup(e => e.GetStreamNameForAggregate<LedgerWriteModel>(ledgerId))
            .Returns(ledgerStreamName);
        _mockEventClient
            .Setup(e => e.AggregateStream<LedgerWriteModel>(ledgerStreamName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as LedgerWriteModel);

        // Act
        var mediator = _services.GetRequiredService<IMediator>();
        var response = await mediator.Send(request);

        // Assert
        Assert.Equal(request.LedgerId, response.LedgerId);
        Assert.Equal(request.LedgerName, response.LedgerName);

        _mockEventClient
            .Verify(e => e.AppendToStreamAsync(
                ledgerStreamName,
                It.IsAny<IWriteModel>(),
                StreamRevision.None,
                response,
                It.IsAny<CancellationToken>()
            ), Times.Once);
    }

    [Fact]
    public async Task Should_Succeed_When_LedgerIsClosed()
    {
        // Prepare
        var ledgerId = Guid.NewGuid();
        var ledgerStreamName = $"ledger-{ledgerId:d}";
        var ledger = new LedgerWriteModel();
        var request = new OpenLedger(ledgerId, "Test Ledger");

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
        var response = await mediator.Send(request);

        // Assert
        Assert.Equal(request.LedgerId, response.LedgerId);
        Assert.Equal(request.LedgerName, response.LedgerName);

        _mockEventClient
            .Verify(e => e.AppendToStreamAsync(
                ledgerStreamName,
                ledger,
                ledger.Version - 2,
                response,
                It.IsAny<CancellationToken>()
            ), Times.Once);
    }
}
