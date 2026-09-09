namespace PpmV2.Domain.Shifts;

public enum ShiftStatus
{
    Draft           = 0,
    PendingApproval = 1,
    Planned         = 2,
    Active          = 3,
    Completed       = 4,
    Cancelled       = 5
}
