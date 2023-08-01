using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public struct MenuOrderInfo
{
    public string name;
    public int amount;
    public string menu_id;
    public int status;//1: 주문, 2: 조리중, 3: 조리완료, 0-주문전
    public string order_id;
}
public struct OrderItem
{
    public int id;//테이블변경, 주문취소인 경우 접수됏는지 확정
    public string tagName;//태그명
    public int orderNo;
    public string tableNo;//테이블명
    public int order_type;//0-매장, 1-포장
    public int is_cancel;//1-취소
    public int is_tableChanged;//1-테이블변경
    public string order_time;//주문시간
    public string finish_time;//완료시간
    public int cook_time;//조리시간 분단위
    public int price;//주문 총 단가
    public int status;//주문 전체 상태
    public string old_tableNo;//테이블변경주문인 경우 이전 테이블명
    public List<MenuOrderInfo> menulist;
    public bool is_call;
}
public struct Set_Info
{
    public string sft_key;
    public string bus_id;
    public int cooking_time;//분단위로
    public int call_type;//0-tablet, 1-monitor, 2-no use
    public int call_recept_option;//0-recept, 1-not recept
}

enum SceneType
{
    splash = 1,
    setting,
    main,
    completed
}

public class CurrencyCodeMapper
{
    private static readonly Dictionary<string, string> SymbolsByCode;

    public static string GetSymbol(string code) { return SymbolsByCode[code]; }

    static CurrencyCodeMapper()
    {
        SymbolsByCode = new Dictionary<string, string>();

        var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                      .Select(x => new RegionInfo(x.LCID));

        foreach (var region in regions)
            if (!SymbolsByCode.ContainsKey(region.ISOCurrencySymbol))
                SymbolsByCode.Add(region.ISOCurrencySymbol, region.CurrencySymbol);
    }
}

public class Global
{
    public static string priceUnit = CurrencyCodeMapper.GetSymbol("KRW");
    //setting information
    public static Set_Info setInfo = new Set_Info();
    public static string old_sftkey;

    public static string udid;
    public static int new_order_cnt = 0;
    public static int cooking_cnt = 0;
    public static int completion_cnt = 0;

    //order list
    public static List<OrderItem> notcompleted_orderlist = new List<OrderItem>();
    public static List<OrderItem> completed_orderlist = new List<OrderItem>();

    //api
    public static string server_address = "";
    public static string api_server_port = "3006";
    public static string api_url = "";
    static string api_prefix = "m-api/kitchen/";

    public static string verify_api = api_prefix + "verify-info";
    public static string get_orderlist_api = api_prefix + "get-orderlist";
    //public static string get_completed_api = api_prefix + "get-completed-orderlist";
    public static string recept_api = api_prefix + "recept-order";
    public static string complete_api = api_prefix + "complete-order";
    public static string call_api = api_prefix + "call-order";
    public static string reback_order_api = api_prefix + "reback";
    public static string set_order_api = api_prefix + "set-order";
    public static string is_check_recept_neworder = api_prefix + "check-order";
    public static string check_softkey_api = api_prefix + "check-softkey";

    public static int cur_orderNo = -1;
    public static bool is_found_udid = false;

    //socket server
    public static string socket_server = "";

    public static void ChangeOrderInfo(int i, OrderItem oinfo, int status)
    {
        //Global.notcompleted_orderlist 변경
        Debug.Log(i + " changed into " + status);
        oinfo.status = status;
        notcompleted_orderlist[i] = oinfo;
        for (int j = 0; j < notcompleted_orderlist[i].menulist.Count; j++)
        {
            MenuOrderInfo minfo = new MenuOrderInfo();
            minfo = notcompleted_orderlist[i].menulist[j];
            minfo.status = status;
            notcompleted_orderlist[i].menulist[j] = minfo;
        }
    }

    public static void ChangeMenuorderInfo(int order_i, int menu_i, int status)
    {
        //Global.notcompleted_orderlist 변경
        OrderItem oinfo = notcompleted_orderlist[order_i];
        MenuOrderInfo m_info = notcompleted_orderlist[order_i].menulist[menu_i];
        m_info.status = status;
        oinfo.menulist[menu_i] = m_info;
        notcompleted_orderlist[order_i] = oinfo;
    }

    public static string GetOrderTimeFormat(string ordertime)
    {
        try
        {
            return string.Format("{0:D2}", Convert.ToDateTime(ordertime).Hour) + ":" + string.Format("{0:D2}", Convert.ToDateTime(ordertime).Minute) + " 주문";
        }
        catch (Exception ex)
        {
            return "";
        }
    }

    public static string GetTimeFormat()
    {
        return string.Format("{0:D2}", DateTime.Now.Hour) + ":" + string.Format("{0:D2}", DateTime.Now.Minute) + ":" + string.Format("{0:D2}", DateTime.Now.Second);
    }

    public static string GetONoFormat(int ono)
    {
        return string.Format("{0:D3}", ono);
    }

    public static string GetCookTimeFormat(int ono)
    {
        if(Math.Abs(ono) > 99)
        {
            return "99";
        }
        return string.Format("{0:D2}", ono);
    }

    public static string GetPriceFormat(int price)
    {
        return priceUnit + string.Format("{0:N0}", price);
    }
}