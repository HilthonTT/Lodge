﻿// <auto-generated />
using System;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lodge.Persistence.Migrations
{
    [DbContext(typeof(LodgeDbContext))]
    partial class LodgeDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Lodge.Domain.Apartements.Apartment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int[]>("Amenities")
                        .IsRequired()
                        .HasColumnType("integer[]")
                        .HasColumnName("amenities");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("image_url");

                    b.Property<DateTime?>("LastBookedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_booked_on_utc");

                    b.Property<int>("MaximumGuestCount")
                        .HasColumnType("integer")
                        .HasColumnName("maximum_guest_count");

                    b.Property<int>("MaximumRoomCount")
                        .HasColumnType("integer")
                        .HasColumnName("maximum_room_count");

                    b.Property<DateTime?>("ModifiedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("modified_on_utc");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id")
                        .HasName("pk_apartments");

                    b.ToTable("apartments", (string)null);
                });

            modelBuilder.Entity("Lodge.Domain.Bookings.Booking", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("ApartmentId")
                        .HasColumnType("uuid")
                        .HasColumnName("apartment_id");

                    b.Property<DateTime?>("CancelledOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("cancelled_on_utc");

                    b.Property<DateTime?>("CompletedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("completed_on_utc");

                    b.Property<DateTime?>("ConfirmedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("confirmed_on_utc");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<DateTime?>("ModifiedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("modified_on_utc");

                    b.Property<DateTime?>("RejectedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("rejected_on_utc");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_bookings");

                    b.HasIndex("ApartmentId")
                        .HasDatabaseName("ix_bookings_apartment_id");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_bookings_user_id");

                    b.ToTable("bookings", (string)null);
                });

            modelBuilder.Entity("Lodge.Domain.Reviews.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("ApartmentId")
                        .HasColumnType("uuid")
                        .HasColumnName("apartment_id");

                    b.Property<Guid>("BookingId")
                        .HasColumnType("uuid")
                        .HasColumnName("booking_id");

                    b.Property<string>("Comment")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("comment");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<DateTime?>("ModifiedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("modified_on_utc");

                    b.Property<int>("Rating")
                        .HasColumnType("integer")
                        .HasColumnName("rating");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_reviews");

                    b.HasIndex("ApartmentId")
                        .HasDatabaseName("ix_reviews_apartment_id");

                    b.HasIndex("BookingId")
                        .HasDatabaseName("ix_reviews_booking_id");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_reviews_user_id");

                    b.ToTable("reviews", (string)null);
                });

            modelBuilder.Entity("Lodge.Domain.Users.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<bool>("Deleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("deleted");

                    b.Property<DateTime?>("DeletedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("deleted_on_utc");

                    b.Property<Guid?>("ImageId")
                        .HasColumnType("uuid")
                        .HasColumnName("image_id");

                    b.Property<DateTime?>("ModifiedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("modified_on_utc");

                    b.Property<string>("_passwordHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("password_hash");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("Lodge.Persistence.Idempotency.IdempotentRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_idempotent_requests");

                    b.ToTable("idempotent_requests", (string)null);
                });

            modelBuilder.Entity("Lodge.Persistence.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("content");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<string>("Error")
                        .HasColumnType("text")
                        .HasColumnName("error");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<DateTime?>("ProcessedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("processed_on_utc");

                    b.HasKey("Id")
                        .HasName("pk_outbox_messages");

                    b.ToTable("outbox_messages", (string)null);
                });

            modelBuilder.Entity("Lodge.Domain.Apartements.Apartment", b =>
                {
                    b.OwnsOne("Lodge.Domain.Apartements.Address", "Address", b1 =>
                        {
                            b1.Property<Guid>("ApartmentId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<string>("City")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("address_city");

                            b1.Property<string>("Country")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("address_country");

                            b1.Property<string>("State")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("address_state");

                            b1.Property<string>("Street")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("address_street");

                            b1.Property<string>("ZipCode")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("address_zip_code");

                            b1.HasKey("ApartmentId");

                            b1.ToTable("apartments");

                            b1.WithOwner()
                                .HasForeignKey("ApartmentId")
                                .HasConstraintName("fk_apartments_apartments_id");
                        });

                    b.OwnsOne("Lodge.Domain.Shared.Money", "CleaningFee", b1 =>
                        {
                            b1.Property<Guid>("ApartmentId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("numeric")
                                .HasColumnName("cleaning_fee_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("cleaning_fee_currency");

                            b1.HasKey("ApartmentId");

                            b1.ToTable("apartments");

                            b1.WithOwner()
                                .HasForeignKey("ApartmentId")
                                .HasConstraintName("fk_apartments_apartments_id");
                        });

                    b.OwnsOne("Lodge.Domain.Shared.Money", "Price", b1 =>
                        {
                            b1.Property<Guid>("ApartmentId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("numeric")
                                .HasColumnName("price_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("price_currency");

                            b1.HasKey("ApartmentId");

                            b1.ToTable("apartments");

                            b1.WithOwner()
                                .HasForeignKey("ApartmentId")
                                .HasConstraintName("fk_apartments_apartments_id");
                        });

                    b.OwnsOne("Lodge.Domain.Apartements.Description", "Description", b1 =>
                        {
                            b1.Property<Guid>("ApartmentId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(1000)
                                .HasColumnType("character varying(1000)")
                                .HasColumnName("description");

                            b1.HasKey("ApartmentId");

                            b1.ToTable("apartments");

                            b1.WithOwner()
                                .HasForeignKey("ApartmentId")
                                .HasConstraintName("fk_apartments_apartments_id");
                        });

                    b.OwnsOne("Lodge.Domain.Apartements.Name", "Name", b1 =>
                        {
                            b1.Property<Guid>("ApartmentId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(256)
                                .HasColumnType("character varying(256)")
                                .HasColumnName("name");

                            b1.HasKey("ApartmentId");

                            b1.ToTable("apartments");

                            b1.WithOwner()
                                .HasForeignKey("ApartmentId")
                                .HasConstraintName("fk_apartments_apartments_id");
                        });

                    b.Navigation("Address")
                        .IsRequired();

                    b.Navigation("CleaningFee")
                        .IsRequired();

                    b.Navigation("Description")
                        .IsRequired();

                    b.Navigation("Name")
                        .IsRequired();

                    b.Navigation("Price")
                        .IsRequired();
                });

            modelBuilder.Entity("Lodge.Domain.Bookings.Booking", b =>
                {
                    b.HasOne("Lodge.Domain.Apartements.Apartment", null)
                        .WithMany()
                        .HasForeignKey("ApartmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_bookings_apartments_apartment_id");

                    b.HasOne("Lodge.Domain.Users.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_bookings_users_user_id");

                    b.OwnsOne("Lodge.Domain.Shared.Money", "AmenitiesUpCharge", b1 =>
                        {
                            b1.Property<Guid>("BookingId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("numeric")
                                .HasColumnName("amenities_up_charge_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("amenities_up_charge_currency");

                            b1.HasKey("BookingId");

                            b1.ToTable("bookings");

                            b1.WithOwner()
                                .HasForeignKey("BookingId")
                                .HasConstraintName("fk_bookings_bookings_id");
                        });

                    b.OwnsOne("Lodge.Domain.Shared.Money", "CleaningFee", b1 =>
                        {
                            b1.Property<Guid>("BookingId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("numeric")
                                .HasColumnName("cleaning_fee_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("cleaning_fee_currency");

                            b1.HasKey("BookingId");

                            b1.ToTable("bookings");

                            b1.WithOwner()
                                .HasForeignKey("BookingId")
                                .HasConstraintName("fk_bookings_bookings_id");
                        });

                    b.OwnsOne("Lodge.Domain.Shared.Money", "PriceForPeriod", b1 =>
                        {
                            b1.Property<Guid>("BookingId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("numeric")
                                .HasColumnName("price_for_period_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("price_for_period_currency");

                            b1.HasKey("BookingId");

                            b1.ToTable("bookings");

                            b1.WithOwner()
                                .HasForeignKey("BookingId")
                                .HasConstraintName("fk_bookings_bookings_id");
                        });

                    b.OwnsOne("Lodge.Domain.Shared.Money", "TotalPrice", b1 =>
                        {
                            b1.Property<Guid>("BookingId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("numeric")
                                .HasColumnName("total_price_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("total_price_currency");

                            b1.HasKey("BookingId");

                            b1.ToTable("bookings");

                            b1.WithOwner()
                                .HasForeignKey("BookingId")
                                .HasConstraintName("fk_bookings_bookings_id");
                        });

                    b.OwnsOne("Lodge.Domain.Bookings.DateRange", "Duration", b1 =>
                        {
                            b1.Property<Guid>("BookingId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.HasKey("BookingId");

                            b1.ToTable("bookings");

                            b1.WithOwner()
                                .HasForeignKey("BookingId")
                                .HasConstraintName("fk_bookings_bookings_id");
                        });

                    b.Navigation("AmenitiesUpCharge")
                        .IsRequired();

                    b.Navigation("CleaningFee")
                        .IsRequired();

                    b.Navigation("Duration")
                        .IsRequired();

                    b.Navigation("PriceForPeriod")
                        .IsRequired();

                    b.Navigation("TotalPrice")
                        .IsRequired();
                });

            modelBuilder.Entity("Lodge.Domain.Reviews.Review", b =>
                {
                    b.HasOne("Lodge.Domain.Apartements.Apartment", null)
                        .WithMany()
                        .HasForeignKey("ApartmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reviews_apartments_apartment_id");

                    b.HasOne("Lodge.Domain.Bookings.Booking", null)
                        .WithMany()
                        .HasForeignKey("BookingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reviews_bookings_booking_id");

                    b.HasOne("Lodge.Domain.Users.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reviews_users_user_id");
                });

            modelBuilder.Entity("Lodge.Domain.Users.User", b =>
                {
                    b.OwnsOne("Lodge.Domain.Users.Email", "Email", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(256)
                                .HasColumnType("character varying(256)")
                                .HasColumnName("email");

                            b1.HasKey("UserId");

                            b1.HasIndex("Value")
                                .IsUnique()
                                .HasDatabaseName("ix_users_email");

                            b1.ToTable("users");

                            b1.WithOwner()
                                .HasForeignKey("UserId")
                                .HasConstraintName("fk_users_users_id");
                        });

                    b.OwnsOne("Lodge.Domain.Users.FirstName", "FirstName", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(128)
                                .HasColumnType("character varying(128)")
                                .HasColumnName("first_name");

                            b1.HasKey("UserId");

                            b1.ToTable("users");

                            b1.WithOwner()
                                .HasForeignKey("UserId")
                                .HasConstraintName("fk_users_users_id");
                        });

                    b.OwnsOne("Lodge.Domain.Users.LastName", "LastName", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(128)
                                .HasColumnType("character varying(128)")
                                .HasColumnName("last_name");

                            b1.HasKey("UserId");

                            b1.ToTable("users");

                            b1.WithOwner()
                                .HasForeignKey("UserId")
                                .HasConstraintName("fk_users_users_id");
                        });

                    b.Navigation("Email")
                        .IsRequired();

                    b.Navigation("FirstName")
                        .IsRequired();

                    b.Navigation("LastName")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
