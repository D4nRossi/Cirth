using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cirth.Infrastructure.Migrations
{
    /// <summary>
    /// Adiciona a coluna gerada <c>content_tsv</c> à tabela <c>chunks</c> + índice GIN.
    /// Necessária para o <see cref="Cirth.Infrastructure.Persistence.Bm25SearchService"/> rodar
    /// <c>plainto_tsquery</c> e <c>ts_rank_cd</c> sobre o texto dos chunks.
    ///
    /// Configuração de FTS: dicionário <c>portuguese</c> (stemming pt-BR/pt-PT, stopwords).
    /// Trocar exige reindexar — não é gratuito.
    /// </summary>
    public partial class AddContentTsvGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE chunks
                    ADD COLUMN content_tsv tsvector
                    GENERATED ALWAYS AS (to_tsvector('portuguese', content)) STORED;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX ix_chunks_content_tsv ON chunks USING GIN (content_tsv);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_chunks_content_tsv;");
            migrationBuilder.Sql("ALTER TABLE chunks DROP COLUMN IF EXISTS content_tsv;");
        }
    }
}
