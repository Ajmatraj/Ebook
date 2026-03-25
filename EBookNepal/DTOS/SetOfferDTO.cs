namespace EBookNepal.DTOS
{
    public class SetOfferDTO
    {
        public string BookId { get; set; }
        public decimal OfferPrice { get; set; }
        public DateTime OfferStartDate { get; set; }
        public DateTime OfferEndDate { get; set; }
    }
}