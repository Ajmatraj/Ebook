using EBookNepal.DTOS;
using EBookNepal.Entities;

namespace EBookNepal.Services.Interfaces
{
    public interface IBookServices
    {
        IEnumerable<BookDTO> GetBooks();
        void AddBook(AddBookDTO bookDto, string coverImageUrl);
        void UpdateBook(UpdateBookDTO bookDto, string uploadPath);
        void AddToWishlist(string userId, string bookId);
        IEnumerable<WishlistDTO> GetWishlist(string userId);
        void RemoveFromWishlist(string wishlistId);
        void SetOffer(SetOfferDTO offerDto);
        IEnumerable<BookDTO> GetPopularBooks();
        IEnumerable<OnSaleBookDTO> GetOnSaleBooks();
    }
}
