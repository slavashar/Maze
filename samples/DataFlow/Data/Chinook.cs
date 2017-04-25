using System;
using System.Collections.Generic;

namespace DataFlow.Data
{
    public interface IChinookSource
    {
        IEnumerable<Album> Album { get; }
        IEnumerable<Artist> Artist { get; }
        IEnumerable<Customer> Customer { get; }
        IEnumerable<Employee> Employee { get; }
        IEnumerable<Genre> Genre { get; }
        IEnumerable<Invoice> Invoice { get; }
        IEnumerable<InvoiceLine> InvoiceLine { get; }
        IEnumerable<MediaType> MediaType { get; }
        IEnumerable<Playlist> Playlist { get; }
        IEnumerable<PlaylistTrack> PlaylistTrack { get; }
        IEnumerable<Track> Track { get; }
    }

    public class Album
    {
        public int AlbumId { get; set; }
        public string Title { get; set; }
        public int ArtistId { get; set; }
    }

    public class Artist
    {
        public int ArtistId { get; set; }
        public string Name { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public int SupportRepId { get; set; }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Title { get; set; }
        public int ReportsTo { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime HireDate { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
    }

    public class Genre
    {
        public int GenreId { get; set; }
        public string Name { get; set; }
    }

    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string BillingAddress { get; set; }
        public string BillingCity { get; set; }
        public string BillingState { get; set; }
        public string BillingCountry { get; set; }
        public string BillingPostalCode { get; set; }
        public decimal Total { get; set; }
    }

    public class InvoiceLine
    {
        public int InvoiceLineId { get; set; }
        public int InvoiceId { get; set; }
        public int TrackId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class MediaType
    {
        public int MediaTypeId { get; set; }
        public string Name { get; set; }
    }

    public class Playlist
    {
        public int PlaylistId { get; set; }
        public string Name { get; set; }
    }

    public class PlaylistTrack
    {
        public int PlaylistId { get; set; }

        public int TrackId { get; set; }
    }

    public class Track
    {
        public int TrackId { get; set; }
        public string Name { get; set; }
        public int AlbumId { get; set; }
        public int MediaTypeId { get; set; }
        public int GenreId { get; set; }
        public string Composer { get; set; }
        public int Milliseconds { get; set; }
        public int Bytes { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
