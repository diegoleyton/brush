using Marmilo.Domain.GameState;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marmilo.Infrastructure.Persistence.Configurations;

public sealed class ChildGameStateConfiguration : IEntityTypeConfiguration<ChildGameState>
{
    public void Configure(EntityTypeBuilder<ChildGameState> builder)
    {
        builder.ToTable("child_game_states");

        builder.HasKey(gameState => gameState.ChildProfileId);

        builder.Property(gameState => gameState.ChildProfileId)
            .HasColumnName("child_profile_id");

        builder.Property(gameState => gameState.CoinsBalance)
            .HasColumnName("coins_balance")
            .IsRequired();

        builder.Property(gameState => gameState.BrushSessionDurationMinutes)
            .HasColumnName("brush_session_duration_minutes")
            .IsRequired();

        builder.Property(gameState => gameState.PendingReward)
            .HasColumnName("pending_reward")
            .IsRequired();

        builder.Property(gameState => gameState.Muted)
            .HasColumnName("muted")
            .IsRequired();

        builder.Property(gameState => gameState.PetStateJson)
            .HasColumnName("pet_state_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(gameState => gameState.RoomStateJson)
            .HasColumnName("room_state_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(gameState => gameState.InventoryStateJson)
            .HasColumnName("inventory_state_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(gameState => gameState.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(gameState => gameState.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
