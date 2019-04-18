using System.Data.Entity.Migrations;

namespace Prover.Core.Migrations
{
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.Certificates",
                    c => new
                    {
                        Id = c.Guid(false),
                        CreatedDateTime = c.DateTime(false),
                        VerificationType = c.String(maxLength: 4000),
                        TestedBy = c.String(maxLength: 4000),
                        Number = c.Long(false)
                    })
                .PrimaryKey(t => t.Id);

            CreateTable(
                    "dbo.Instruments",
                    c => new
                    {
                        Id = c.Guid(false),
                        TestDateTime = c.DateTime(false),
                        Type = c.Int(false),
                        CertificateId = c.Guid(),
                        InstrumentData = c.String(maxLength: 4000)
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Certificates", t => t.CertificateId)
                .Index(t => t.CertificateId);

            CreateTable(
                    "dbo.Temperatures",
                    c => new
                    {
                        Id = c.Guid(false),
                        InstrumentId = c.Guid(false),
                        InstrumentData = c.String(maxLength: 4000)
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Instruments", t => t.Id)
                .Index(t => t.Id);

            CreateTable(
                    "dbo.TemperatureTests",
                    c => new
                    {
                        Id = c.Guid(false),
                        TemperatureId = c.Guid(false),
                        IsVolumeTestTemperature = c.Boolean(false),
                        TestLevel = c.Int(false),
                        Gauge = c.Double(false),
                        InstrumentData = c.String(maxLength: 4000)
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Temperatures", t => t.TemperatureId)
                .Index(t => t.TemperatureId);

            CreateTable(
                    "dbo.Volumes",
                    c => new
                    {
                        Id = c.Guid(false),
                        PulseACount = c.Int(false),
                        PulseBCount = c.Int(false),
                        AppliedInput = c.Double(false),
                        InstrumentId = c.Guid(false),
                        TestInstrumentData = c.String(maxLength: 4000),
                        InstrumentData = c.String(maxLength: 4000)
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Instruments", t => t.Id)
                .Index(t => t.Id);
        }

        public override void Down()
        {
            DropForeignKey("dbo.Volumes", "Id", "dbo.Instruments");
            DropForeignKey("dbo.TemperatureTests", "TemperatureId", "dbo.Temperatures");
            DropForeignKey("dbo.Temperatures", "Id", "dbo.Instruments");
            DropForeignKey("dbo.Instruments", "CertificateId", "dbo.Certificates");
            DropIndex("dbo.Volumes", new[] {"Id"});
            DropIndex("dbo.TemperatureTests", new[] {"TemperatureId"});
            DropIndex("dbo.Temperatures", new[] {"Id"});
            DropIndex("dbo.Instruments", new[] {"CertificateId"});
            DropTable("dbo.Volumes");
            DropTable("dbo.TemperatureTests");
            DropTable("dbo.Temperatures");
            DropTable("dbo.Instruments");
            DropTable("dbo.Certificates");
        }
    }
}