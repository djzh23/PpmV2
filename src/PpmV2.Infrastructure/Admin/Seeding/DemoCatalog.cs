using PpmV2.Domain.Shifts;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Admin.Seeding;

/// <summary>
/// Fictional demo content for screenshots. Legacy addresses exist only so an
/// existing database can recognize the previous accounts. New rows use the
/// current address.
/// </summary>
public static class DemoCatalog
{
    public const string LegacyLocationName = "Wohnanlage Elbpark";

    public static readonly Guid LegacyShiftId = new("11111111-0001-0000-0000-000000000000");

    public static readonly DemoPerson[] People =
    [
        new("lena.hartung@example.com", "koord1@test.com", "Lena", "Hartung", UserRole.Coordinator),
        new("jonas.rehberg@example.com", "koord2@test.com", "Jonas", "Rehberg", UserRole.Coordinator),
        new("mara.quell@example.com", "koord3@test.com", "Mara", "Quell", UserRole.Coordinator),
        new("tim.adler@example.com", "fest1@test.com", "Tim", "Adler", UserRole.Festmitarbeiter),
        new("sofia.bruck@example.com", "fest2@test.com", "Sofia", "Bruck", UserRole.Festmitarbeiter),
        new("henrik.pauli@example.com", "fest3@test.com", "Henrik", "Pauli", UserRole.Festmitarbeiter),
        new("nora.feld@example.com", "hon1@test.com", "Nora", "Feld", UserRole.Honorarkraft),
        new("emil.kranz@example.com", "hon2@test.com", "Emil", "Kranz", UserRole.Honorarkraft),
        new("pia.sund@example.com", "hon3@test.com", "Pia", "Sund", UserRole.Honorarkraft),
    ];

    public static readonly DemoPlace[] Places =
    [
        new("Wohnpunkt Falkenhain", "Falkenau", "Falkenhorst 4"),
        new("Haus Nebelwiese", "Nebelmark", "Nebelwiese 12"),
        new("Hof Kieselgrund", "Kiesgrund", "Kiesbach 3"),
        new("Unterkunft Silberrain", "Silberhang", "Silberrain 9"),
        new("Wohnbereich Mondlicht", "Mondau", "Mondfeld 18"),
        new("Haus Tannbrück", "Tannfeld", "Tannbrück 6"),
        new("Hof Quellenhang", "Quellenort", "Quellenhang 21"),
        new("Wohnpunkt Buchenrain", "Buchenrain", "Buchenau 2"),
        new("Unterkunft Weidengrund", "Weidental", "Weidengrund 8"),
        new("Wohnbereich Drosselkamp", "Drosselbach", "Drosselkamp 11"),
        new("Hof Marlenbach", "Marbachtal", "Marbach 1"),
        new("Wohnbereich Eulengrund", "Eulenfeld", "Eulengrund 5"),
    ];

    /// <summary>
    /// First id is the marker that this catalog was already inserted.
    /// </summary>
    public static readonly DemoShiftPlan[] Shifts =
    [
        new(ShiftId(1), "Frühdienst Falkenhain", "Übergabe am Morgen und Begehung der Gemeinschaftsflächen.", "Wohnpunkt Falkenhain", ShiftStatus.Completed, -4, 8, -4, 16, false,
            Member("tim.adler@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("nora.feld@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(2), "Spätdienst Nebelwiese", "Betreuung am Nachmittag und Unterstützung bei der Ausgabe.", "Haus Nebelwiese", ShiftStatus.Completed, -2, 14, -2, 22, false,
            Member("sofia.bruck@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("emil.kranz@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(3), "Nachtdienst Kieselgrund", "Nachtrunde und Kontrolle der Zugänge.", "Hof Kieselgrund", ShiftStatus.Completed, -1, 22, 0, 6, false,
            Member("henrik.pauli@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("pia.sund@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(4), "Tagdienst Silberrain", "Laufende Tagesschicht mit Präsenz im Haus.", "Unterkunft Silberrain", ShiftStatus.Active, 0, 0, 0, 0, true,
            Member("sofia.bruck@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("nora.feld@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(5), "Tagdienst Mondlicht", "Schicht wurde vor Beginn abgesagt.", "Wohnbereich Mondlicht", ShiftStatus.Cancelled, 2, 8, 2, 16, false,
            Member("henrik.pauli@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("emil.kranz@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Declined)),
        new(ShiftId(6), "Frühdienst Tannbrück", "Frühschicht. Das Team wird noch zusammengestellt.", "Haus Tannbrück", ShiftStatus.Draft, 3, 6, 3, 14, false,
            Member("tim.adler@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(7), "Spätdienst Quellenhang", "Spätschicht im Entwurf.", "Hof Quellenhang", ShiftStatus.Draft, 5, 14, 5, 22, false,
            Member("sofia.bruck@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("pia.sund@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(8), "Wochenenddienst Buchenrain", "Wochenendeinsatz im Entwurf.", "Wohnpunkt Buchenrain", ShiftStatus.Draft, 8, 10, 8, 18, false,
            Member("henrik.pauli@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(9), "Tagdienst Weidengrund", "Team ist vorgeschlagen. Rückmeldungen stehen aus.", "Unterkunft Weidengrund", ShiftStatus.PendingApproval, 10, 8, 10, 16, false,
            Member("tim.adler@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("emil.kranz@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Invited),
            Member("pia.sund@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Invited)),
        new(ShiftId(10), "Spätdienst Drosselkamp", "Ein Teil des Teams hat zugesagt.", "Wohnbereich Drosselkamp", ShiftStatus.PendingApproval, 12, 14, 12, 22, false,
            Member("sofia.bruck@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("henrik.pauli@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted),
            Member("nora.feld@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Invited)),
        new(ShiftId(11), "Frühdienst Marlenbach", "Team ist vollständig und der Einsatz ist geplant.", "Hof Marlenbach", ShiftStatus.Planned, 15, 6, 15, 14, false,
            Member("henrik.pauli@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("nora.feld@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted),
            Member("emil.kranz@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(12), "Nachtdienst Falkenhain", "Geplante Nachtschicht mit festem Team.", "Wohnpunkt Falkenhain", ShiftStatus.Planned, 17, 22, 18, 6, false,
            Member("tim.adler@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("pia.sund@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted)),
        new(ShiftId(13), "Tagdienst Eulengrund", "Geplanter Tagdienst mit Leitung und Unterstützung.", "Wohnbereich Eulengrund", ShiftStatus.Planned, 20, 8, 20, 16, false,
            Member("sofia.bruck@example.com", ShiftRole.Leader, ParticipantConfirmationStatus.Accepted),
            Member("henrik.pauli@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted),
            Member("emil.kranz@example.com", ShiftRole.Member, ParticipantConfirmationStatus.Accepted),
            Member("lena.hartung@example.com", ShiftRole.Support, ParticipantConfirmationStatus.Accepted)),
    ];

    public static readonly DemoAssignment[] Assignments =
    [
        new("tim.adler@example.com", ["Wohnpunkt Falkenhain", "Haus Tannbrück", "Unterkunft Weidengrund", "Hof Marlenbach"]),
        new("sofia.bruck@example.com", ["Haus Nebelwiese", "Unterkunft Silberrain", "Hof Quellenhang", "Wohnbereich Drosselkamp", "Wohnbereich Eulengrund"]),
        new("henrik.pauli@example.com", ["Hof Kieselgrund", "Wohnbereich Mondlicht", "Wohnpunkt Buchenrain"]),
    ];

    public static Guid MarkerShiftId => Shifts[0].Id;

    private static Guid ShiftId(int n) => Guid.Parse($"22222222-{n:D4}-4000-8000-000000000000");

    private static DemoMember Member(string email, ShiftRole role, ParticipantConfirmationStatus status) => new(email, role, status);
}

public sealed record DemoPerson(string Email, string LegacyEmail, string Firstname, string Lastname, UserRole Role);

public sealed record DemoPlace(string Name, string District, string Address);

public sealed record DemoMember(string Email, ShiftRole Role, ParticipantConfirmationStatus Status);

public sealed record DemoShiftPlan(
    Guid Id,
    string Title,
    string? Description,
    string LocationName,
    ShiftStatus Status,
    int StartDayOffset,
    int StartHour,
    int EndDayOffset,
    int EndHour,
    bool AroundNow,
    params DemoMember[] Members);

public sealed record DemoAssignment(string Email, string[] LocationNames);
