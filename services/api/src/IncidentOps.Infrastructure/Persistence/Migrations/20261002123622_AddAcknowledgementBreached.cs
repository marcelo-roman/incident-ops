using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentOps.Infrastructure.Persistence.Migrations
{
    public partial class AddAcknowledgementBreached : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcknowledgementBreached",
                table: "Incidents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [Incidents] SET [AcknowledgementBreached] = 1 WHERE [EscalationLevel] > 1 OR [AcknowledgedAt] > [AckDueAt]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgementBreached",
                table: "Incidents");
        }
    }
}
