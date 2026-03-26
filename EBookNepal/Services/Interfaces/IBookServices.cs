using EBookNepal.DTOS;

namespace EBookNepal.Services.Interfaces
{
    public interface IBookServices
    {
        IEnumerable<BookDTO> GetBooks();

        void AddBook(AddBookDTO bookDto, string coverImageUrl);

        void UpdateBook(UpdateBookDTO bookDto, string coverImageUrl);

        void AddToWishlist(string bookId);

        IEnumerable<WishlistDTO> GetWishlist();

        void RemoveFromWishlist(string wishlistId);

        void SetOffer(SetOfferDTO offerDto);

        IEnumerable<BookDTO> GetPopularBooks();

        IEnumerable<OnSaleBookDTO> GetOnSaleBooks();
    }
}