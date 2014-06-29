namespace Hangfire.Sample.Highlighter.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSnippet : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Snippet",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Source = c.String(nullable: false),
                        HighlightedSource = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        HighlightedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Snippet");
        }
    }
}
