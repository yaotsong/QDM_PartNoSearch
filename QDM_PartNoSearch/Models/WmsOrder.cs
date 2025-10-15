namespace QDM_PartNoSearch.Models
{
    public class WmsOrder
    {
        //訂單狀態
        public string status_code { get; set; }
        //訂單狀態名稱
        public string status_name { get; set; }
        //商品編碼
        public string sku { get; set; }
        //商品名稱
        public string name {  get; set; }
        //數量
        public int qty { get; set; }
        //倉別
        public string warehouse { get; set; }
        //貨號
        public string item_no { get; set; }//新增貨號
    }
}
