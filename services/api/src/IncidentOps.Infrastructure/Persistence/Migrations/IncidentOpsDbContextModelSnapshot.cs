using System;
using IncidentOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace IncidentOps.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(IncidentOpsDbContext))]
    partial class IncidentOpsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.31")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.HasSequence<int>("IncidentNumbers")
                .StartsAt(1001L);

            modelBuilder.Entity("IncidentOps.Domain.Catalog.Service", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("OwnerTeam")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Tier")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.HasKey("Id");

                    b.ToTable("Services", (string)null);
                });

            modelBuilder.Entity("IncidentOps.Domain.Incidents.Incident", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("AcknowledgedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("AlertFingerprint")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("Assignee")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("nvarchar(4000)");

                    b.Property<int>("EscalationLevel")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("MitigatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Number")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("ResolvedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("RootCause")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<string>("ServiceId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("Severity")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("AlertFingerprint")
                        .IsUnique()
                        .HasDatabaseName("IX_Incidents_OpenAlertFingerprint")
                        .HasFilter("[AlertFingerprint] IS NOT NULL AND [Status] <> 'Resolved'");

                    b.HasIndex("Number")
                        .IsUnique();

                    b.HasIndex("ServiceId");

                    b.HasIndex("Status");

                    b.ToTable("Incidents", (string)null);
                });

            modelBuilder.Entity("IncidentOps.Domain.Incidents.TimelineEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Actor")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTimeOffset>("At")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("IncidentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Kind")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("nvarchar(4000)");

                    b.Property<int>("Sequence")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("IncidentId", "Sequence")
                        .IsUnique();

                    b.ToTable("TimelineEntries", (string)null);
                });

            modelBuilder.Entity("IncidentOps.Domain.OnCall.Engineer", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<int>("RotationOrder")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Role", "RotationOrder");

                    b.ToTable("Engineers", (string)null);
                });

            modelBuilder.Entity("IncidentOps.Infrastructure.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Attempts")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("DeadLetteredAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("LastError")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTimeOffset>("NextAttemptAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("OccurredAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("ProcessedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("Sequence")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Sequence"));

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.HasKey("Id");

                    b.HasIndex("ProcessedAt", "DeadLetteredAt", "NextAttemptAt", "Sequence");

                    b.ToTable("OutboxMessages", (string)null);
                });

            modelBuilder.Entity("IncidentOps.Domain.Incidents.Incident", b =>
                {
                    b.HasOne("IncidentOps.Domain.Catalog.Service", null)
                        .WithMany()
                        .HasForeignKey("ServiceId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.OwnsOne("IncidentOps.Domain.Sla.SlaClock", "Sla", b1 =>
                        {
                            b1.Property<Guid>("IncidentId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<DateTimeOffset>("AckDueAt")
                                .HasColumnType("datetimeoffset")
                                .HasColumnName("AckDueAt");

                            b1.Property<DateTimeOffset>("AckWindowStartsAt")
                                .HasColumnType("datetimeoffset")
                                .HasColumnName("AckWindowStartsAt");

                            b1.Property<bool>("AcknowledgementBreached")
                                .HasColumnType("bit")
                                .HasColumnName("AcknowledgementBreached");

                            b1.Property<DateTimeOffset>("ResolveDueAt")
                                .HasColumnType("datetimeoffset")
                                .HasColumnName("ResolveDueAt");

                            b1.Property<DateTimeOffset>("StartedAt")
                                .HasColumnType("datetimeoffset")
                                .HasColumnName("CreatedAt");

                            b1.HasKey("IncidentId");

                            b1.HasIndex("StartedAt")
                                .HasDatabaseName("IX_Incidents_CreatedAt");

                            b1.ToTable("Incidents");

                            b1.WithOwner()
                                .HasForeignKey("IncidentId");
                        });

                    b.Navigation("Sla")
                        .IsRequired();
                });

            modelBuilder.Entity("IncidentOps.Domain.Incidents.TimelineEntry", b =>
                {
                    b.HasOne("IncidentOps.Domain.Incidents.Incident", null)
                        .WithMany("Timeline")
                        .HasForeignKey("IncidentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IncidentOps.Domain.Incidents.Incident", b =>
                {
                    b.Navigation("Timeline");
                });
#pragma warning restore 612, 618
        }
    }
}
