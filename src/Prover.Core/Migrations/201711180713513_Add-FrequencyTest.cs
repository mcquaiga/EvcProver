namespace Prover.Core.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFrequencyTest : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FrequencyTests",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        AfterTestData = c.String(maxLength: 4000),
                        MainRotorPulseCount = c.Long(nullable: false),
                        SenseRotorPulseCount = c.Long(nullable: false),
                        MechanicalOutputFactor = c.Long(nullable: false),
                        VerificationTestId = c.Guid(nullable: false),
                        InstrumentData = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.VerificationTests", t => t.Id)
                .Index(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FrequencyTests", "Id", "dbo.VerificationTests");
            DropIndex("dbo.FrequencyTests", new[] { "Id" });
            DropTable("dbo.FrequencyTests");
        }
    }
}
