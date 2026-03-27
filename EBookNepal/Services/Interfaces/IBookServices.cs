using EBookNepal.DTOS;

namespace EBookNepal.Services.Interfaces
{
    public interface IBookServices
    {
        // Books
        IEnumerable<BookDTO> GetBooks();
        IEnumerable<BookDTO> GetBooksBySeller(string sellerId);
        BookDTO GetBookById(string bookId);
        void AddBook(AddBookDTO bookDto, string coverImageUrl);
        void UpdateBook(UpdateBookDTO bookDto, string coverImageUrl);
        void DeleteBook(string bookId);

        // Wishlist
        void AddToWishlist(string bookId);
        IEnumerable<WishlistDTO> GetWishlist();
        void RemoveFromWishlist(string wishlistId);

        // Cart
        void AddToCart(string bookId, int quantity);
        IEnumerable<CartDTO> GetCart();
        void RemoveFromCart(string cartItemId);

        // Offers
        void SetOffer(SetOfferDTO offerDto);

        // Discovery
        IEnumerable<BookDTO> GetPopularBooks();
        IEnumerable<OnSaleBookDTO> GetOnSaleBooks();
    }
}