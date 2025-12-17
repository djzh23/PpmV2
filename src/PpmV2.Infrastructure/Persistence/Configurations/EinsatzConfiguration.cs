using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Assignments;
using PpmV2.Domain.Einsaetze;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Infrastructure.Persistence.Configurations;

//public class EinsatzConfiguration : IEntityTypeConfiguration<Einsatz>
//{
//    //public void Configure(EntityTypeBuilder<Einsatz> builder)
//    //{
//    //    builder.ToTable("Einsaetze");

//    //    builder.HasKey(e => e.Id);

//    //    builder.Property(e => e.Title)
//    //        .IsRequired()
//    //        .HasMaxLength(200);

//    //    builder.Property(e => e.Description)
//    //        .HasMaxLength(2000);

//    //    builder.Property(e => e.StartAtUtc)
//    //        .IsRequired();

//    //    builder.Property(e => e.EndAtUtc);

//    //    // Enum als int (Default), explizit ist sauber
//    //    builder.Property(e => e.Status)
//    //        .HasConversion<int>()
//    //        .IsRequired();

//    //    builder.Property(e => e.LocationId)
//    //        .IsRequired();

//    //    // FK auf Locations (Location ist bereits implementiert)
//    //    builder.HasOne<Location>()              // Navigation brauchst du hier nicht zwingend
//    //        .WithMany()
//    //        .HasForeignKey(e => e.LocationId)
//    //        .OnDelete(DeleteBehavior.Restrict); // empfohlen: Location nicht kaskadierend löschen

//    //    // 1:n Einsatz -> Participants
//    //    builder.HasMany(e => e.Participants)
//    //        .WithOne() // keine Navigation in EinsatzParticipant notwendig
//    //        .HasForeignKey(p => p.EinsatzId)
//    //        .OnDelete(DeleteBehavior.Cascade);  // wenn Einsatz gelöscht wird, Participants weg
//    //}
//}
