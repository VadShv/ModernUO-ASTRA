using System;
using Server;
using Server.Accounting;
using Server.Logging;

namespace Server.Custom;

public static class CreatePlayerAccount
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CreatePlayerAccount));

    public static void Configure()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted()
    {
        var username = "player";
        var password = "12345678";

        if (Accounts.GetAccount(username) != null)
        {
            logger.Information("CreatePlayerAccount: Account '{User}' already exists.", username);
            return;
        }

        var account = new Account(username, password);
        logger.Information("CreatePlayerAccount: Created account '{User}' with access level {Level}.",
            username, account.AccessLevel);
    }
}
