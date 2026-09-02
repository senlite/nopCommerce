using FluentMigrator;
using Nop.Data.Migrations;

namespace TwinParticles.CheckEngine.Infrastructure.Migrations;

[NopMigration("2026-08-19 12:00:00", "TwinParticles.CheckEngine Horizon 4 workshop completion (credit statements, labour rates)", MigrationProcessType.Installation)]
public sealed class Horizon4WorkshopCompletion : Migration
{
    public override void Up()
    {
        if (!Schema.Table("TP_CE_WorkshopLabourRate").Exists())
        {
            Create.Table("TP_CE_WorkshopLabourRate")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("WorkshopAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopAccount", "Id")
                .WithColumn("OperationCode").AsString(32).NotNullable()
                .WithColumn("HourlyRate").AsDecimal(18, 4).NotNullable();

            Create.Index("IX_TP_CE_WorkshopLabourRate_Account_Code")
                .OnTable("TP_CE_WorkshopLabourRate")
                .OnColumn("WorkshopAccountId").Ascending()
                .OnColumn("OperationCode").Ascending();
        }

        if (!Schema.Table("TP_CE_WorkshopCreditStatement").Exists())
        {
            Create.Table("TP_CE_WorkshopCreditStatement")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("WorkshopAccountId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopAccount", "Id")
                .WithColumn("PeriodStartUtc").AsDateTime2().NotNullable()
                .WithColumn("PeriodEndUtc").AsDateTime2().NotNullable()
                .WithColumn("OpeningBalance").AsDecimal(18, 4).NotNullable()
                .WithColumn("InvoicedTotal").AsDecimal(18, 4).NotNullable()
                .WithColumn("ClosingBalance").AsDecimal(18, 4).NotNullable()
                .WithColumn("CreatedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_WorkshopCreditStatement_Account")
                .OnTable("TP_CE_WorkshopCreditStatement")
                .OnColumn("WorkshopAccountId").Ascending();
        }

        if (!Schema.Table("TP_CE_WorkshopCreditStatementLine").Exists())
        {
            Create.Table("TP_CE_WorkshopCreditStatementLine")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("StatementId").AsInt32().NotNullable().ForeignKey("TP_CE_WorkshopCreditStatement", "Id")
                .WithColumn("JobId").AsInt32().NotNullable()
                .WithColumn("OrderId").AsInt32().Nullable()
                .WithColumn("PartsTotal").AsDecimal(18, 4).NotNullable()
                .WithColumn("LabourTotal").AsDecimal(18, 4).NotNullable()
                .WithColumn("LineTotal").AsDecimal(18, 4).NotNullable()
                .WithColumn("InvoicedUtc").AsDateTime2().NotNullable();

            Create.Index("IX_TP_CE_WorkshopCreditStatementLine_Statement")
                .OnTable("TP_CE_WorkshopCreditStatementLine")
                .OnColumn("StatementId").Ascending();
        }
    }

    public override void Down()
    {
    }
}
