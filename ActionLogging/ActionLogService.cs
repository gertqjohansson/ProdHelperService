using ProdHelperService.Auth;

namespace ProdHelperService.ActionLogging;

// Only adds a tracked entity - deliberately does NOT call SaveChangesAsync itself, so the audit
// row commits atomically in the same SaveChangesAsync/transaction as the business-data change it
// documents, rather than as a separate follow-up write that could succeed or fail independently.
public class ActionLogService(ApplicationDbContext db) : IActionLogService
{
    public void Record(string actionType, string section, string madeByUser, DateTime actionTimeUtc, string? oldValuesJson, string newValuesJson)
    {
        db.ActionLogs.Add(new ActionLog
        {
            ActionTime = actionTimeUtc,
            ActionType = actionType,
            MadeByUser = madeByUser,
            Section = section,
            OldValues = oldValuesJson,
            NewValues = newValuesJson,
        });
    }
}
