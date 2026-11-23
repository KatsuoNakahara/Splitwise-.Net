using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Splitwise.Application.Abstractions;
using Splitwise.Domain.Entities;
using Splitwise.Infrastructure.Identity;

namespace Splitwise.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Group> Groups { get; set; } = null!;

    public DbSet<GroupMember> GroupMembers { get; set; } = null!;

    public DbSet<Expense> Expenses { get; set; } = null!;

    public DbSet<ExpenseSplit> ExpenseSplits { get; set; } = null!;

    public DbSet<Settlement> Settlements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Group>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.CreatedByUserId).IsRequired();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GroupMember>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.UserId });
            e.HasOne(x => x.Group).WithMany(g => g.Members).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Expense>(e =>
        {
            e.Property(x => x.Description).IsRequired().HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Group).WithMany(g => g.Expenses).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PaidByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExpenseSplit>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Expense).WithMany(x => x.Splits).HasForeignKey(x => x.ExpenseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Settlement>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Group).WithMany(g => g.Settlements).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
