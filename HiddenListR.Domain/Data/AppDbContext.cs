using HiddenListR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HiddenListR.Domain.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventMember> EventMembers => Set<EventMember>();
    public DbSet<WishList> WishLists => Set<WishList>();
    public DbSet<WishListItem> WishListItems => Set<WishListItem>();
    public DbSet<WishListItemPurchase> WishListItemPurchases => Set<WishListItemPurchase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();

        modelBuilder.Entity<EventMember>()
            .HasKey(em => new { em.UserId, em.EventId });

        modelBuilder.Entity<EventMember>()
            .HasOne(em => em.User)
            .WithMany(u => u.EventMembers)
            .HasForeignKey(em => em.UserId);

        modelBuilder.Entity<EventMember>()
            .HasOne(em => em.Event)
            .WithMany(e => e.EventMembers)
            .HasForeignKey(em => em.EventId);

        modelBuilder.Entity<WishList>()
            .HasIndex(wl => new { wl.EventId, wl.OwnerUserId })
            .IsUnique();

        modelBuilder.Entity<WishList>()
            .HasOne(wl => wl.Event)
            .WithMany(e => e.WishLists)
            .HasForeignKey(wl => wl.EventId);

        modelBuilder.Entity<WishList>()
            .HasOne(wl => wl.OwnerUser)
            .WithMany(u => u.WishLists)
            .HasForeignKey(wl => wl.OwnerUserId);

        modelBuilder.Entity<WishListItem>()
            .HasOne(i => i.WishList)
            .WithMany(wl => wl.Items)
            .HasForeignKey(i => i.WishListId);

        modelBuilder.Entity<WishListItemPurchase>()
            .HasIndex(p => p.WishListItemId)
            .IsUnique();

        modelBuilder.Entity<WishListItemPurchase>()
            .HasOne(p => p.WishListItem)
            .WithOne(i => i.Purchase)
            .HasForeignKey<WishListItemPurchase>(p => p.WishListItemId);

        modelBuilder.Entity<WishListItemPurchase>()
            .HasOne(p => p.PurchasedByUser)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.PurchasedByUserId);
    }
}
