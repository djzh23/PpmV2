using Moq;
using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Application.Shifts.Queries.GetShiftDetails;
using PpmV2.Domain.Shifts;

namespace PpmV2.Tests.Shifts;

public class GetShiftDetailsHandlerTests
{
    private readonly Mock<IShiftDetailsQuery> _queryMock = new();
    private readonly GetShiftDetailsHandler _handler;

    public GetShiftDetailsHandlerTests()
    {
        _handler = new GetShiftDetailsHandler(_queryMock.Object);
    }

    private static ShiftDetailsDto BuildDto(Guid id) => new()
    {
        Id = id,
        Title = "Test Einsatz",
        StartAtUtc = DateTime.UtcNow.AddDays(1),
        Status = ShiftStatus.Draft,
        Location = new ShiftLocationDto { Id = Guid.NewGuid(), Name = "HQ", District = "Berlin" },
        Participants = [],
        Readiness = "not_ready",
        MissingRequirements = ["leader"]
    };

    [Fact]
    public async Task Handle_ReturnsDto_WhenShiftExists()
    {
        var id = Guid.NewGuid();
        _queryMock
            .Setup(q => q.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildDto(id));

        var result = await _handler.Handle(new GetShiftDetailsQuery(id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("Test Einsatz", result.Title);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenShiftNotFound()
    {
        var id = Guid.NewGuid();
        _queryMock
            .Setup(q => q.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftDetailsDto?)null);

        var result = await _handler.Handle(new GetShiftDetailsQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ForwardsExactId_ToQuery()
    {
        var id = Guid.NewGuid();
        _queryMock
            .Setup(q => q.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftDetailsDto?)null);

        await _handler.Handle(new GetShiftDetailsQuery(id), CancellationToken.None);

        _queryMock.Verify(q => q.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken_ToQuery()
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        _queryMock
            .Setup(q => q.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftDetailsDto?)null);

        await _handler.Handle(new GetShiftDetailsQuery(id), cts.Token);

        _queryMock.Verify(q => q.GetByIdAsync(id, cts.Token), Times.Once);
    }
}
