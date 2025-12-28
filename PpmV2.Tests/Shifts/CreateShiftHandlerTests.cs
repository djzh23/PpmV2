using Moq;
using PpmV2.Application.Common.Exceptions;
using PpmV2.Application.Shifts.Commands.Creation;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Tests.Shifts;

public class CreateShiftHandlerTests
{
    private readonly Mock<IShiftRepository> _repoMock;
    private readonly CreateShiftHandler _handler;

    public CreateShiftHandlerTests()
    {
        _repoMock = new Mock<IShiftRepository>();
        _handler = new CreateShiftHandler(_repoMock.Object);
    }

    /// <summary>
    /// Helper method for creating a valid default command.
    /// Returns a CreateShiftCommand with all required and valid values.
    /// </summary>
    private CreateShiftCommand CreateValidCommand()
    {
        var locationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);

        return new CreateShiftCommand(
            Title: "Test Shift",
            Description: "Test Description",
            StartAtUtc: startTime,
            EndAtUtc: startTime.AddHours(2),
            LocationId: locationId,
            Participants: new List<CreateShiftParticipantDto>
            {
                new(userId, ShiftRole.Leader)
            }
        );
    }

    [Fact]
    public async Task Handle_Should_Create_Shift_With_Correct_Data()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var locationId = cmd.LocationId;
        var userId = cmd.Participants.First().UserId;

        _repoMock.Setup(r => r.LocationExistsAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.CountExistingUsersAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Shift? capturedShift = null;
        IReadOnlyCollection<ShiftParticipant>? capturedParticipants = null;

        _repoMock
            .Setup(r => r.AddAsync(
                It.IsAny<Shift>(),
                It.IsAny<IReadOnlyCollection<ShiftParticipant>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Shift, IReadOnlyCollection<ShiftParticipant>, CancellationToken>((shift, participants, _) =>
            {
                capturedShift = shift;
                capturedParticipants = participants;
            });

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        // Verify Shift entity values
        Assert.NotNull(capturedShift);
        Assert.Equal(cmd.Title.Trim(), capturedShift.Title); // Title is trimmed internally
        Assert.Equal(cmd.Description, capturedShift.Description);
        Assert.Equal(cmd.StartAtUtc, capturedShift.StartAtUtc);
        Assert.Equal(cmd.EndAtUtc, capturedShift.EndAtUtc);
        Assert.Equal(cmd.LocationId, capturedShift.LocationId);
        Assert.Equal(ShiftStatus.Draft, capturedShift.Status);

        // Verify participants
        Assert.NotNull(capturedParticipants);
        Assert.Single(capturedParticipants);
        Assert.Equal(userId, capturedParticipants.First().UserId);
        Assert.Equal(ShiftRole.Leader, capturedParticipants.First().Role);
        Assert.Equal(result, capturedParticipants.First().ShiftId);

        // Verify repository interactions
        _repoMock.Verify(r => r.LocationExistsAsync(locationId, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.CountExistingUsersAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Shift>(), It.IsAny<IReadOnlyCollection<ShiftParticipant>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_Title_Is_Empty()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with { Title = "" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("title", exception.Errors.Keys);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Shift>(), It.IsAny<IReadOnlyCollection<ShiftParticipant>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_Title_Is_Whitespace()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with { Title = "   " };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("title", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_EndAtUtc_Is_Before_StartAtUtc()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with { EndAtUtc = validCmd.StartAtUtc.AddHours(-1) };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("endAtUtc", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_EndAtUtc_Equals_StartAtUtc()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with { EndAtUtc = validCmd.StartAtUtc };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("endAtUtc", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_No_Participants()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with { Participants = new List<CreateShiftParticipantDto>() };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("participants", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_Null_Participants()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with { Participants = null! };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("participants", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_No_Leader()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with
        {
            Participants = new List<CreateShiftParticipantDto>
            {
                new(Guid.NewGuid(), ShiftRole.Member) // No leader assigned
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("participants.role", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_Multiple_Leaders()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with
        {
            Participants = new List<CreateShiftParticipantDto>
            {
                new(Guid.NewGuid(), ShiftRole.Leader),
                new(Guid.NewGuid(), ShiftRole.Leader) // Multiple leaders
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("participants.role", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_UserId_Is_Empty()
    {
        // Arrange
        var validCmd = CreateValidCommand();
        var cmd = validCmd with
        {
            Participants = new List<CreateShiftParticipantDto>
            {
                new(Guid.Empty, ShiftRole.Leader) // Empty UserId
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("participants.userId", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_Location_Does_Not_Exist()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var locationId = cmd.LocationId;

        _repoMock.Setup(r => r.LocationExistsAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("locationId", exception.Errors.Keys);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Shift>(), It.IsAny<IReadOnlyCollection<ShiftParticipant>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_ValidationException_When_User_Does_Not_Exist()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var locationId = cmd.LocationId;
        var userId = cmd.Participants.First().UserId;

        _repoMock.Setup(r => r.LocationExistsAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.CountExistingUsersAsync(It.Is<List<Guid>>(l => l.Contains(userId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // User does not exist

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("participants.userId", exception.Errors.Keys);
    }

    [Fact]
    public async Task Handle_Should_Allow_StartAtUtc_In_Past_When_No_Validation_Exists()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var locationId = cmd.LocationId;

        // Set StartAtUtc to a past date
        var cmdWithPastStart = cmd with { StartAtUtc = DateTime.UtcNow.AddHours(-1) };

        _repoMock.Setup(r => r.LocationExistsAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.CountExistingUsersAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert
        // Note: The handler currently does not validate StartAtUtc being in the past.
        // This test documents the current behavior.
        var result = await _handler.Handle(cmdWithPastStart, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_Should_Allow_Duplicate_Participants_When_No_Validation_Exists()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var userId = cmd.Participants.First().UserId;
        var locationId = cmd.LocationId;

        // Same user appears multiple times
        var cmdWithDuplicates = cmd with
        {
            Participants = new List<CreateShiftParticipantDto>
            {
                new(userId, ShiftRole.Leader),
                new(userId, ShiftRole.Member)
            }
        };

        _repoMock.Setup(r => r.LocationExistsAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.CountExistingUsersAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert
        // Note: Duplicate participants are currently allowed.
        // This test documents existing behavior.
        var result = await _handler.Handle(cmdWithDuplicates, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
    }
}
