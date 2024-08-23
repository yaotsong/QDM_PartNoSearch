namespace QDM_PartNoSearch.Models
{
    public class WmsApi
    {
        //SKU
        public string Id { get; set; }
        //商品名稱
        public string Name { get; set; }
        //庫存數
        public int Stock {  get; set; }
        //出貨數
        public int Qty { get; set; }
    }
}
