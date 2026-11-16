# Orleans schema for PostgreSQL

Orleans does not create its tables. These scripts are copied unchanged from
[dotnet/orleans](https://github.com/dotnet/orleans/tree/v10.4.0/src/AdoNet), tag `v10.4.0`, the version pinned in `Directory.Packages.props`.

| File | Source in `src/AdoNet` |
|---|---|
| `PostgreSQL-Main.sql` | `Shared/PostgreSQL-Main.sql`: the `OrleansQuery` table every other script fills |
| `PostgreSQL-Clustering.sql` | `Orleans.Clustering.AdoNet/PostgreSQL-Clustering.sql`: membership table |
| `PostgreSQL-Reminders.sql` | `Orleans.Reminders.AdoNet/PostgreSQL-Reminders.sql`: reminder table |
| `PostgreSQL-Persistence.sql` | `Orleans.Persistence.AdoNet/PostgreSQL-Persistence.sql`: grain state table |

`../init.sh` applies them: the `orleans` database gets Main, Clustering and Reminders, every module database gets Main and Persistence.

When Orleans is upgraded, replace the files with the ones of the new tag, including the upgrade scripts in `Migrations/` for databases that already exist. Do not edit them by hand.
