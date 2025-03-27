namespace QDM_PartNoSearch.Models
{
    public class WmsProduct
    {
        //SKU
        public string Id { get; set; }
        public string PartNo { get; set; }
        //商品名稱
        public string Name { get; set; }
        //庫存數
        public int Stock {  get; set; }
        //出貨數
        public int Qty { get; set; }
        //倉別
        public string Warehouse { get; set; }
    }
}
