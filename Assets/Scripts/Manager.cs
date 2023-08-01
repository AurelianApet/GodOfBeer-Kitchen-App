using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using SimpleJSON;
using SocketIO;

public class Manager : MonoBehaviour
{
    public GameObject splashObj;
    public GameObject settingObj;
    public GameObject mainListObj;
    public GameObject backBtn;
    public GameObject title;
    public GameObject logo;
    public GameObject setBtn;

    //setting ui
    public InputField bus_id;
    public InputField pos_ip;
    public InputField sft_key;
    public InputField cooking_time;
    public ToggleGroup call_type;
    public Text error_msg;
    public Text call_txt;
    public GameObject bottomForm;
    public GameObject callForm;
    public GameObject callSetBtn;

    //main ui
    public GameObject item;
    public GameObject menu_item;
    public GameObject tableItem;
    public GameObject itemParent;
    public GameObject menu_itemParent;
    public GameObject rebackBtn;
    public GameObject pozhangNotice;
    public GameObject cancelNotice;
    public GameObject error_popup;
    public GameObject call_popup;
    public GameObject receptBtn;
    public GameObject callBtn;
    public GameObject completeBtn;
    public GameObject menuPanel;
    public GameObject line;
    public GameObject tablePanel;
    public GameObject timeField;
    public GameObject titleField;

    public Text err_msg;
    public Text order_time;
    public Text table_no;
    public Text order_no;
    public Text current_time;
    public Text order_price;
    public Text new_order_Cnt_Txt;
    public Text cooking_Cnt_Txt;
    public Text complete_Cnt_Txt;

    public GameObject socketPrefab;
    GameObject socketObj;
    private SocketIOComponent socket;
    List<GameObject> itemObj = new List<GameObject>();
    GameObject[] menuitemObj;
    bool is_loading = false;
    int old_i = -1;
    int curOrderSeq = 0;
    string curTagName = "";
    string curTableName = "";
    float time = 0f;

    bool is_send_request = false;
    SceneType curSceneType;
    bool is_socket_open = false;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        Screen.orientation = ScreenOrientation.Landscape;
        curSceneType = SceneType.splash;
        ShowScene();
        Global.setInfo.bus_id = PlayerPrefs.GetString("bus_id");
        Global.server_address = PlayerPrefs.GetString("ip");
        Global.setInfo.sft_key = PlayerPrefs.GetString("sft_key");
        Global.old_sftkey = PlayerPrefs.GetString("old_sftkey");
        Global.setInfo.cooking_time = PlayerPrefs.GetInt("cooking_time");
        yield return new WaitForSeconds(0.1f);
        if (Global.server_address == "")
        {
            Debug.Log("no saved data");
            curSceneType = SceneType.setting;
            ShowScene();
        }
        else
        {
            Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
            Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
            WWWForm form = new WWWForm();
            form.AddField("softkey", Global.setInfo.sft_key);
            WWW www = new WWW(Global.api_url + Global.check_softkey_api, form);
            StartCoroutine(ProcessCheckSoftkey(www));
        }
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void showCallForm()
    {
        bottomForm.SetActive(false);
        callForm.SetActive(true);
        callSetBtn.SetActive(false);
        Debug.Log("saved call type = " + Global.setInfo.call_type);
        if (Global.setInfo.call_type == 0)
        {
            //tablet
            call_type.transform.Find("tablet").GetComponent<Toggle>().isOn = true;
        }
        else if (Global.setInfo.call_type == 1)
        {
            //monitor
            call_type.transform.Find("monitor").GetComponent<Toggle>().isOn = true;
        }
        else if (Global.setInfo.call_type == 2)
        {
            //no use
            call_type.transform.Find("unuse").GetComponent<Toggle>().isOn = true;
        }
        else
        {
            call_type.transform.Find("all").GetComponent<Toggle>().isOn = true;
        }
        Debug.Log("saved recept option = " + Global.setInfo.call_recept_option);

        if (Global.setInfo.call_recept_option == 0)
        {
            callForm.transform.Find("recept_option/use").GetComponent<Toggle>().isOn = true;
            callForm.transform.Find("recept_option/unuse").GetComponent<Toggle>().isOn = false;
        }
        else
        {
            callForm.transform.Find("recept_option/use").GetComponent<Toggle>().isOn = false;
            callForm.transform.Find("recept_option/unuse").GetComponent<Toggle>().isOn = true;
        }
    }

    public void onCallPopup()
    {
        call_popup.SetActive(false);
    }

    void LoadSettingInfo()
    {
        bus_id.text = Global.setInfo.bus_id;
        pos_ip.text = Global.server_address;
        sft_key.text = Global.setInfo.sft_key;
        if(Global.setInfo.cooking_time == 0)
        {
            Global.setInfo.cooking_time = 10;
        }
        cooking_time.text = Global.setInfo.cooking_time.ToString();
    }

    IEnumerator ProcessVerify(WWW www, int c_type, int recpet_option)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                try
                {
                    Global.server_address = pos_ip.text;
                    Global.setInfo.bus_id = bus_id.text;
                    Global.setInfo.cooking_time = int.Parse(cooking_time.text);
                    Global.setInfo.sft_key = sft_key.text;
                    Global.setInfo.call_type = c_type;
                    Global.setInfo.call_recept_option = recpet_option;
                }
                catch (Exception ex)
                {
                }
                PlayerPrefs.SetString("bus_id", bus_id.text);
                PlayerPrefs.SetString("ip", Global.server_address);
                Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
                Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
                PlayerPrefs.SetString("sft_key", sft_key.text);
                PlayerPrefs.SetInt("call_type", c_type);
                PlayerPrefs.SetInt("call_recept_option", recpet_option);
                Global.old_sftkey = sft_key.text;
                PlayerPrefs.SetString("old_sftkey", Global.old_sftkey);
                try
                {
                    PlayerPrefs.SetInt("cooking_time", int.Parse(cooking_time.text));
                }
                catch (Exception ex)
                {
                    PlayerPrefs.SetInt("cooking_time", 10);
                }
                PlayerPrefs.Save();
                if (socketObj != null)
                {
                    is_socket_open = false;
                    socket.Close();
                    socket.OnDestroy();
                    socket.OnApplicationQuit();
                    DestroyImmediate(socketObj);
                    socketObj = null;
                }
                socketObj = Instantiate(socketPrefab);
                socket = socketObj.GetComponent<SocketIOComponent>();
                socket.On("open", socketOpen);
                socket.On("login", GetUDIDFromServer);
                socket.On("createOrder", CreateOrerEventProcess);
                socket.On("refreshEvent", RefreshEvent);
                socket.On("callClient", CallClientProcess);
                socket.On("error", socketError);
                socket.On("close", socketClose);
                curSceneType = SceneType.main;
                ShowScene();
            }
            else
            {
                error_msg.text = jsonNode["msg"];
                error_popup.SetActive(true);
            }
        }
        else
        {
            error_msg.text = "인증에 실패하였습니다.";
            error_popup.SetActive(true);
        }
    }

    public void onSettingBackBtn()
    {
        if (callSetBtn.activeSelf)
        {
            //api 통해서 인증
            if(bus_id.text == "" || sft_key.text == "")
            {
                error_msg.text = "설정값들을 정확히 입력하세요.";
                error_popup.SetActive(true);
                return;
            }
            if(pos_ip.text == "")
            {
                error_popup.SetActive(true);
                error_msg.text = "ip를 정확히 입력하세요.";
                return;
            }
            Global.server_address = pos_ip.text;
            Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
            Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
            WWWForm form = new WWWForm();
            form.AddField("bus_id", bus_id.text);
            form.AddField("sft_key", sft_key.text);
            Debug.Log("old sft key = " + Global.old_sftkey);
            if (Global.old_sftkey != null && Global.old_sftkey != "")
            {
                form.AddField("old_sftkey", Global.old_sftkey);
            }
            int c_type = 0;
            if (call_type.transform.Find("tablet").GetComponent<Toggle>().isOn)
            {
                c_type = 0;
            }
            else if (call_type.transform.Find("monitor").GetComponent<Toggle>().isOn)
            {
                c_type = 1;
            }
            else if (call_type.transform.Find("unuse").GetComponent<Toggle>().isOn)
            {
                c_type = 2;
            }
            else
            {
                c_type = 3;//동시호출
            }
            form.AddField("call_type", c_type);
            Debug.Log("call type = " + c_type);
            int recpet_option = 0;
            if (callForm.transform.Find("recept_option/unuse").GetComponent<Toggle>().isOn)
            {
                recpet_option = 1;
            }
            Debug.Log("recept option = " + recpet_option);
            WWW www = new WWW(Global.api_url + Global.verify_api, form);
            StartCoroutine(ProcessVerify(www, c_type, recpet_option));
        }
        else
        {
            callSetBtn.SetActive(true);
            callForm.SetActive(false);
            bottomForm.SetActive(true);
        }
    }

    void ShowScene()
    {
        splashObj.SetActive(false);
        settingObj.SetActive(false);
        mainListObj.SetActive(false);
        Debug.Log("CurSceneType : " + curSceneType);
        switch (curSceneType)
        {
            case SceneType.splash:
                {
                    splashObj.SetActive(true); break;
                };
            case SceneType.setting:
                {
                    settingObj.SetActive(true);
                    LoadSettingInfo();
                    break;
                };
            case SceneType.main:
                {
                    mainListObj.SetActive(true);
                    StartCoroutine(DrawNotCompletedOrderlist());
                    break;
                };
            case SceneType.completed:
                {
                    mainListObj.SetActive(true);
                    StartCoroutine(DrawCompletedOrderList());
                    break;
                }
        }
    }

    IEnumerator DrawNotCompletedOrderlist()
    {
        //clear
        while (itemParent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(itemParent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            yield return new WaitForSeconds(0.01f);
        }
        itemObj.Clear();
        backBtn.SetActive(false);
        title.SetActive(false);
        logo.SetActive(true);
        setBtn.SetActive(true);
        while (true)
        {
            if (!is_loading)
            {
                yield return new WaitForSeconds(0.01f);
            }
            else
            {
                break;
            }
        }
        order_price.text = "";
        old_i = -1;
        Debug.Log("set old_i = -1");
        curOrderSeq = 0;
        curTagName = "";
        curTableName = "";

        StartCoroutine("loadTimer");

        //UI에 구현(완료되지 않은 주문들만 골라서)
        for (int i = 0; i < Global.notcompleted_orderlist.Count; i++)
        {
            if (Global.notcompleted_orderlist[i].is_tableChanged == 1)
            {
                GameObject itemOj = Instantiate(tableItem, new Vector3(0, 0, 0), Quaternion.identity);
                itemOj.transform.SetParent(itemParent.transform);
                itemOj.transform.Find("orderno").GetComponent<Text>().text = Global.GetONoFormat(Global.notcompleted_orderlist[i].orderNo);
                itemOj.transform.Find("id").GetComponent<Text>().text = Global.notcompleted_orderlist[i].tableNo;
                itemOj.transform.Find("content").GetComponent<Text>().text = "테이블 이동 -> " + Global.notcompleted_orderlist[i].tableNo;
                itemOj.transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_new");
                int _i = i;
                itemOj.GetComponent<Button>().onClick.RemoveAllListeners();
                itemOj.GetComponent<Button>().onClick.AddListener(delegate () { SelectItem(_i); });

                itemOj.transform.localPosition = Vector3.zero;
                if (itemParent.transform.childCount > 1)
                {
                    itemOj.transform.GetComponent<RectTransform>().sizeDelta =
                        new Vector2(itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x,
                        itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.y);
                }
                else
                {
                    itemOj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-itemParent.transform.GetComponent<RectTransform>().sizeDelta.x / 2,
                        itemParent.transform.GetComponent<RectTransform>().sizeDelta.y / 4);
                }
                itemOj.transform.localScale = Vector3.one;
                itemObj.Add(itemOj);
            }
            else
            {
                GameObject itemOj = Instantiate(item, new Vector3(0, 0, 0), Quaternion.identity);
                itemOj.transform.SetParent(itemParent.transform);
                string menuContent = "";
                string menuAmount = "";
                for (int j = 0; j < Global.notcompleted_orderlist[i].menulist.Count; j++)
                {
                    if (j == 0)
                    {
                        menuContent = Global.notcompleted_orderlist[i].menulist[j].name;
                        menuAmount = Global.notcompleted_orderlist[i].menulist[j].amount.ToString();
                    }
                    else if (j == 1)
                    {
                        menuContent += "\n" + Global.notcompleted_orderlist[i].menulist[j].name;
                        menuAmount += "\n" + Global.notcompleted_orderlist[i].menulist[j].amount.ToString();
                    }
                    else if (j == 2)
                    {
                        menuContent += "\n...";
                    }
                }
                itemOj.transform.Find("id").GetComponent<Text>().text = Global.notcompleted_orderlist[i].orderNo.ToString();
                itemOj.transform.Find("orderno").GetComponent<Text>().text = Global.GetONoFormat(Global.notcompleted_orderlist[i].orderNo);
                itemOj.transform.Find("tableno").GetComponent<Text>().text = Global.notcompleted_orderlist[i].tableNo;
                itemOj.transform.Find("content").GetComponent<Text>().text = menuContent;
                itemOj.transform.Find("amount").GetComponent<Text>().text = menuAmount;
                //요리시간에 따라 박스배경색 바꾸기
                if (Global.notcompleted_orderlist[i].status == 1)
                {
                    itemOj.transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_new");
                }
                else if (Global.notcompleted_orderlist[i].status == 2)
                {
                    if ((DateTime.Now - Convert.ToDateTime(Global.notcompleted_orderlist[i].order_time)).TotalSeconds > Global.setInfo.cooking_time * 60)
                    {
                        itemOj.transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking1");
                    }
                    else
                    {
                        itemOj.transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking2");
                        int _ii = i;
                        StartCoroutine(UpdateBoxColor(itemOj));
                    }
                }
                Debug.Log("pozhang = " + Global.notcompleted_orderlist[i].order_type);
                if (Global.notcompleted_orderlist[i].is_cancel == 1)
                {
                    itemOj.transform.Find("cancel").gameObject.SetActive(true);
                }
                else if (Global.notcompleted_orderlist[i].order_type == 1)
                {
                    itemOj.transform.Find("pozhang").gameObject.SetActive(true);
                }
                int _i = i;
                itemOj.GetComponent<Button>().onClick.RemoveAllListeners();
                itemOj.GetComponent<Button>().onClick.AddListener(delegate () { SelectItem(_i); });

                itemOj.transform.localPosition = Vector3.zero;
                if (itemParent.transform.childCount > 1)
                {
                    itemOj.transform.GetComponent<RectTransform>().sizeDelta =
                        new Vector2(itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x,
                        itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.y);
                }
                else
                {
                    //Debug.Log(itemParent.transform.GetComponent<RectTransform>().rect.width);
                    //Debug.Log(itemParent.transform.GetComponent<RectTransform>().rect.height);
                    itemOj.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-itemParent.transform.GetComponent<RectTransform>().sizeDelta.x / 2,
                        itemParent.transform.GetComponent<RectTransform>().sizeDelta.y / 4);
                }
                itemOj.transform.localPosition = Vector3.zero;
                itemOj.transform.localScale = Vector3.one;
                itemObj.Add(itemOj);
            }
        }

        new_order_Cnt_Txt.text = Global.new_order_cnt.ToString();
        cooking_Cnt_Txt.text = Global.cooking_cnt.ToString();
        complete_Cnt_Txt.text = Global.completion_cnt.ToString();
        if (Global.notcompleted_orderlist.Count > 0)
        {
            Debug.Log("cur order no = " + Global.cur_orderNo);
            if (Global.cur_orderNo != -1)
            {
                SelectItem(Global.cur_orderNo);
                ShowMenuPanel(true);
            }
            else
            {
                SelectItem(0);
                ShowMenuPanel(true);
            }
        }
        else
        {
            ShowMenuPanel(false);
            callBtn.SetActive(false);
            completeBtn.SetActive(false);
            receptBtn.SetActive(false);
        }
    }

    void LoadOrderInfo()
    {
        Global.cooking_cnt = 0;
        Global.new_order_cnt = 0;
        Global.completion_cnt = 0;
        Global.completed_orderlist.Clear();
        Global.notcompleted_orderlist.Clear();
        is_loading = false;
        order_price.text = "";
        //api로부터 주문정보 가져오기
        WWWForm form = new WWWForm();
        WWW www = new WWW(Global.api_url + Global.get_orderlist_api, form);
        StartCoroutine(GetOrderlistFromApi(www));
    }

    IEnumerator GetOrderlistFromApi(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            Debug.Log("response = " + www.text);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            JSONNode c_list = JSON.Parse(jsonNode["orderlist"].ToString()/*.Replace("\"", "")*/);
            int item_cnt = c_list.Count;
            Debug.Log("notcompleted cnt = " + item_cnt);
            for (int i = 0; i < item_cnt; i++)
            {
                OrderItem oitem = new OrderItem();
                oitem.orderNo = c_list[i]["orderNo"].AsInt;
                oitem.tagName = c_list[i]["tagName"];
                oitem.is_tableChanged = c_list[i]["is_tableChanged"];
                oitem.tableNo = c_list[i]["tableNo"];
                oitem.status = c_list[i]["status"].AsInt;
                oitem.is_call = false;
                if (oitem.is_tableChanged == 1)
                {
                    oitem.old_tableNo = c_list[i]["old_tableNo"];
                    oitem.id = c_list[i]["id"];
                }
                else
                {
                    oitem.order_type = c_list[i]["order_type"].AsInt;
                    oitem.is_cancel = c_list[i]["is_cancel"].AsInt;//1-취소주문
                    oitem.price = c_list[i]["price"].AsInt;
                    oitem.order_time = c_list[i]["order_time"];
                    if (oitem.is_cancel == 1)
                    {
                        oitem.id = c_list[i]["id"];
                        JSONNode m_list = JSON.Parse(c_list[i]["menulist"].ToString());
                        oitem.menulist = new List<MenuOrderInfo>();
                        for (int j = 0; j < m_list.Count; j++)
                        {
                            MenuOrderInfo minfo = new MenuOrderInfo();
                            minfo.order_id = m_list[j]["order_id"];
                            minfo.menu_id = m_list[j]["product_id"];
                            minfo.name = m_list[j]["product_name"];
                            minfo.amount = m_list[j]["quantity"].AsInt;
                            minfo.status = 1;
                            oitem.menulist.Add(minfo);
                        }
                    }
                    else
                    {
                        JSONNode m_list = JSON.Parse(c_list[i]["menulist"].ToString());
                        oitem.menulist = new List<MenuOrderInfo>();
                        for (int j = 0; j < m_list.Count; j++)
                        {
                            MenuOrderInfo minfo = new MenuOrderInfo();
                            minfo.order_id = m_list[j]["order_id"];
                            minfo.menu_id = m_list[j]["menu_id"];
                            minfo.name = m_list[j]["name"];
                            minfo.amount = m_list[j]["amount"].AsInt;
                            minfo.status = m_list[j]["status"].AsInt;
                            oitem.menulist.Add(minfo);
                        }
                    }
                }

                if (oitem.status == 1)
                {
                    Global.new_order_cnt++;
                    Global.notcompleted_orderlist.Add(oitem);
                }
                else if (oitem.status == 2)
                {
                    Global.cooking_cnt++;
                    Global.notcompleted_orderlist.Add(oitem);
                }
            }

            c_list = JSON.Parse(jsonNode["completed_orderlist"].ToString()/*.Replace("\"", "")*/);
            item_cnt = c_list.Count;
            Debug.Log("completed cnt = " + item_cnt);
            for (int i = 0; i < item_cnt; i++)
            {
                OrderItem oitem = new OrderItem();
                oitem.orderNo = c_list[i]["orderNo"].AsInt;
                oitem.tagName = c_list[i]["tagName"];
                oitem.price = c_list[i]["total_price"].AsInt;
                oitem.order_type = c_list[i]["order_type"].AsInt;
                oitem.is_call = false;
                oitem.status = 3;
                oitem.is_cancel = c_list[i]["is_cancel"].AsInt;//1-취소주문
                oitem.order_time = c_list[i]["reg_datetime"];
                oitem.finish_time = c_list[i]["finish_time"];
                oitem.cook_time = c_list[i]["cooktime"];
                oitem.tableNo = c_list[i]["table_name"];
                oitem.id = c_list[i]["id"];
                JSONNode m_list = JSON.Parse(c_list[i]["menulist"].ToString());
                oitem.menulist = new List<MenuOrderInfo>();
                for (int j = 0; j < m_list.Count; j++)
                {
                    MenuOrderInfo minfo = new MenuOrderInfo();
                    minfo.order_id = m_list[j]["order_id"];
                    minfo.menu_id = m_list[j]["menu_id"];
                    minfo.name = m_list[j]["name"];
                    minfo.amount = m_list[j]["amount"].AsInt;
                    minfo.status = m_list[j]["status"].AsInt;
                    oitem.menulist.Add(minfo);
                }

                Global.completed_orderlist.Add(oitem);
                Global.completion_cnt++;
            }

            c_list = JSON.Parse(jsonNode["cancelList"].ToString());
            item_cnt = c_list.Count;
            Debug.Log("cancellist cnt = " + item_cnt);
            for (int i = 0; i < item_cnt; i++)
            {
                OrderItem oitem = new OrderItem();
                oitem.orderNo = c_list[i]["orderNo"].AsInt;
                oitem.price = c_list[i]["total_price"].AsInt;
                oitem.order_type = c_list[i]["order_type"].AsInt;
                oitem.finish_time = c_list[i]["finish_time"];
                oitem.order_time = c_list[i]["reg_datetime"];
                oitem.is_call = false;
                oitem.tableNo = c_list[i]["table_name"];
                oitem.id = c_list[i]["id"];
                oitem.status = 3;
                oitem.is_cancel = 1;//1-취소주문

                JSONNode m_list = JSON.Parse(c_list[i]["menulist"].ToString());
                oitem.menulist = new List<MenuOrderInfo>();
                for (int j = 0; j < m_list.Count; j++)
                {
                    MenuOrderInfo minfo = new MenuOrderInfo();
                    minfo.order_id = m_list[j]["order_id"];
                    minfo.menu_id = m_list[j]["menu_id"];
                    minfo.name = m_list[j]["name"];
                    minfo.amount = m_list[j]["amount"].AsInt;
                    minfo.status = m_list[j]["status"].AsInt;
                    oitem.menulist.Add(minfo);
                }

                Global.completed_orderlist.Add(oitem);
            }
            is_loading = true;
        }
    }

    IEnumerator DrawCompletedOrderList()
    {
        //clear
        while (itemParent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(itemParent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            yield return new WaitForSeconds(0.01f);
        }
        itemObj.Clear();
        receptBtn.SetActive(false);
        callBtn.SetActive(false);
        completeBtn.SetActive(false);
        while (true)
        {
            if (!is_loading)
            {
                yield return new WaitForSeconds(0.01f);
            }
            else
            {
                break;
            }
        }
        backBtn.SetActive(true);
        title.SetActive(true);
        logo.SetActive(false);
        setBtn.SetActive(false);
        Debug.Log("CompletedInfo Scene showed");
        old_i = -1;
        Debug.Log("set old_i = -1");
        curOrderSeq = 0;
        curTagName = "";
        curTableName = "";

        Debug.Log(Global.completed_orderlist.Count);
        for (int i = 0; i < Global.completed_orderlist.Count; i++)
        {
            Debug.Log(Global.completed_orderlist[i].status);
            string menuContent = "";
            string menuAmount = "";
            for (int j = 0; j < Global.completed_orderlist[i].menulist.Count; j++)
            {
                if (j == 0)
                {
                    menuContent = Global.completed_orderlist[i].menulist[j].name;
                    menuAmount = Global.completed_orderlist[i].menulist[j].amount.ToString();
                }
                else if (j == 1)
                {
                    menuContent += "\n" + Global.completed_orderlist[i].menulist[j].name;
                    menuAmount += "\n" + Global.completed_orderlist[i].menulist[j].amount.ToString();
                }
                else if (j == 2)
                {
                    menuContent += "\n...";
                }
            }

            //UI에 구현
            GameObject tmp = Instantiate(item);
            tmp.transform.SetParent(itemParent.transform);
            try
            {
                tmp.transform.Find("orderno").GetComponent<Text>().text = Global.GetONoFormat(Global.completed_orderlist[i].orderNo);
                tmp.transform.Find("tableno").GetComponent<Text>().text = Global.completed_orderlist[i].tableNo;
                tmp.transform.Find("content").GetComponent<Text>().text = menuContent;
                tmp.transform.Find("amount").GetComponent<Text>().text = menuAmount;
                //요리시간에 따라 박스배경색 바꾸기
                tmp.transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_complete");
                if (Global.completed_orderlist[i].is_cancel == 1)
                {
                    tmp.transform.Find("cancel").gameObject.SetActive(true);
                }
                else if (Global.completed_orderlist[i].order_type == 1)
                {
                    tmp.transform.Find("pozhang").gameObject.SetActive(true);
                }
                int _i = i;
                tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                tmp.GetComponent<Button>().onClick.AddListener(delegate () { SelectCompletedItem(_i); });
                tmp.transform.localPosition = Vector3.zero;
                if (itemParent.transform.childCount > 1)
                {
                    tmp.transform.GetComponent<RectTransform>().sizeDelta =
                        new Vector2(itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x,
                        itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.y);
                }
                else
                {
                    tmp.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(-itemParent.transform.GetComponent<RectTransform>().sizeDelta.x / 2,
                        itemParent.transform.GetComponent<RectTransform>().sizeDelta.y / 4);
                }
                tmp.transform.localPosition = Vector3.zero;
                tmp.transform.localScale = Vector3.one;
                itemObj.Add(tmp);
            }
            catch (Exception ex)
            {

            }
        }

        if (Global.completed_orderlist.Count > 0)
        {
            SelectCompletedItem(0);
            ShowMenuPanel(true);
        }
        else
        {
            ShowMenuPanel(false);
        }
    }

    IEnumerator ShowCompletedMenuItem(int i)
    {
        while (menu_itemParent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(menu_itemParent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {

            }
            yield return new WaitForSeconds(0.01f);
        }
        menuitemObj = null;
        menuitemObj = new GameObject[Global.completed_orderlist[i].menulist.Count];
        callBtn.SetActive(false);
        completeBtn.SetActive(false);
        receptBtn.SetActive(false);
        int is_c = Global.completed_orderlist[i].is_cancel;
        if (is_c == 1)
        {
            cancelNotice.SetActive(true);
            rebackBtn.SetActive(false);
        }
        else
        {
            cancelNotice.SetActive(false);
            rebackBtn.SetActive(true);
        }
        for (int j = 0; j < Global.completed_orderlist[i].menulist.Count; j++)
        {
            menuitemObj[j] = Instantiate(menu_item);
            menuitemObj[j].transform.SetParent(menu_itemParent.transform);
            try
            {
                menuitemObj[j].transform.Find("name").GetComponent<Text>().text = Global.completed_orderlist[i].menulist[j].name;
                if (is_c == 1)
                {
                    menuitemObj[j].transform.Find("amount").GetComponent<Text>().text = "-" + Global.completed_orderlist[i].menulist[j].amount.ToString();
                    menuitemObj[j].transform.Find("status").GetComponent<Image>().sprite = null;
                }
                else
                {
                    menuitemObj[j].transform.Find("amount").GetComponent<Text>().text = Global.completed_orderlist[i].menulist[j].amount.ToString();
                    menuitemObj[j].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("check");
                }

            }
            catch (Exception ex)
            {

            }
            menuitemObj[j].transform.localPosition = Vector3.zero;
            if (menu_itemParent.transform.childCount > 1)
            {
                menuitemObj[j].transform.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(menu_itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x,
                    menu_itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.y);
            }
            else
            {
                menuitemObj[j].transform.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(-menu_itemParent.transform.GetComponent<RectTransform>().sizeDelta.x,
                    -menu_itemParent.transform.GetComponent<RectTransform>().sizeDelta.y / 6);
            }
            menuitemObj[j].transform.localScale = Vector3.one;
        }

        //is_loading = true;
        old_i = i;
        curOrderSeq = Global.completed_orderlist[i].orderNo;
        curTagName = Global.completed_orderlist[i].tagName;
    }

    public void SelectCompletedItem(int i)
    {
        Debug.Log(i + "completed selected: old_i = " + old_i);
        if (old_i == i)
            return;
        try
        {
            if (old_i != -1)
            {
                itemObj[old_i].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_complete");
            }
            itemObj[i].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_complete_selected");
        }
        catch (Exception)
        {

        }
        ShowMenuPanel(true);
        if (Global.completed_orderlist[i].order_type == 1)
        {
            pozhangNotice.SetActive(true);
        }
        else
        {
            pozhangNotice.SetActive(false);
        }
        order_time.text = Global.GetOrderTimeFormat(Global.completed_orderlist[i].order_time);
        table_no.text = Global.completed_orderlist[i].tableNo;
        order_no.text = Global.GetONoFormat(Global.completed_orderlist[i].orderNo);
        order_price.text = Global.GetPriceFormat(Global.completed_orderlist[i].price);
        if (Global.completed_orderlist[i].is_cancel == 1)
        {
            current_time.text = Global.GetOrderTimeFormat(Global.completed_orderlist[i].finish_time);
        }
        else
        {
            current_time.text = Global.GetCookTimeFormat(Global.completed_orderlist[i].cook_time) + "분 경과";
        }
        StartCoroutine(ShowCompletedMenuItem(i));
    }

    IEnumerator ProcessCheckSoftkey(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                LoadOrderInfo();
                Global.old_sftkey = Global.setInfo.sft_key;
                PlayerPrefs.SetString("old_sftkey", Global.old_sftkey);
                if (socketObj != null)
                {
                    is_socket_open = false;
                    socket.Close();
                    socket.OnDestroy();
                    socket.OnApplicationQuit();
                    DestroyImmediate(socketObj);
                    socketObj = null;
                }
                socketObj = Instantiate(socketPrefab);
                socket = socketObj.GetComponent<SocketIOComponent>();
                socket.On("open", socketOpen);
                socket.On("login", GetUDIDFromServer);
                socket.On("createOrder", CreateOrerEventProcess);
                socket.On("refreshEvent", RefreshEvent);
                socket.On("callClient", CallClientProcess);
                socket.On("error", socketError);
                socket.On("close", socketClose); curSceneType = SceneType.main;
                ShowScene();
                itemParent.transform.GetComponent<GridLayoutGroup>().cellSize = new Vector2(GameObject.Find("Canvas/home/left/Scroll View/Viewport").transform.GetComponent<RectTransform>().rect.width / 2,
                    GameObject.Find("Canvas/home/left/Scroll View/Viewport").transform.GetComponent<RectTransform>().rect.height / 4);
                itemParent.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.Log("Software Key 값을 확인하세요.");
                curSceneType = SceneType.setting;
                ShowScene();
            }
        }
        else
        {
            Debug.Log("Software Key 값을 확인하세요.");
            curSceneType = SceneType.setting;
            ShowScene();
        }
    }

    void FixedUpdate()
    {
        if (!Input.anyKey)
        {
            time += Time.deltaTime;
        }
        else
        {
            if (time != 0f)
            {
                GameObject.Find("touch").GetComponent<AudioSource>().Play();
                time = 0f;
            }
        }
    }

    IEnumerator loadTimer()
    {
        while (true)
        {
            if (old_i != -1 && curSceneType == SceneType.main)
            {
                Debug.Log("old_i = " + old_i);
                if (Global.notcompleted_orderlist[old_i].is_tableChanged == 1
                    || Global.notcompleted_orderlist[old_i].is_cancel == 1)
                {
                    current_time.text = "";
                }
                else
                {
                    current_time.text = Global.GetCookTimeFormat(Convert.ToInt32((DateTime.Now - Convert.ToDateTime(Global.notcompleted_orderlist[old_i].order_time)).TotalMinutes)) + "분 경과";
                }
            }
            yield return new WaitForSeconds(60);
        }
    }

    IEnumerator UpdateBoxColor(GameObject boxObj)
    {
        int status = 2;
        while (status < 3)
        {
            yield return new WaitForSeconds(60);
            string order_no = boxObj.transform.Find("id").GetComponent<Text>().text;
            string order_time = "";
            bool is_sel = false;
            for (int i = 0; i < Global.notcompleted_orderlist.Count; i++)
            {
                if (order_no == Global.notcompleted_orderlist[i].orderNo.ToString())
                {
                    order_time = Global.notcompleted_orderlist[i].order_time;
                    status = Global.notcompleted_orderlist[i].status;
                    if (i == Global.cur_orderNo)
                    {
                        is_sel = true;
                    }
                    break;
                }
            }
            if (status == 2)
            {
                if ((DateTime.Now - Convert.ToDateTime(order_time)).TotalSeconds > Global.setInfo.cooking_time * 60)
                {
                    if (is_sel)
                    {
                        boxObj.transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking1_selected");
                    }
                    else
                    {
                        boxObj.transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking1");
                    }
                    break;
                }
            }
        }
    }

    void ShowMenuPanel(bool status)
    {
        menuPanel.SetActive(status);
        line.SetActive(status);
        order_price.gameObject.SetActive(status);
        timeField.SetActive(status);
        titleField.SetActive(status);
    }

    void ShowPanelType(bool is_tableChanged)
    {
        tablePanel.SetActive(is_tableChanged);
        ShowMenuPanel(!is_tableChanged);
        ShowBtnType(is_tableChanged);
    }

    void ShowBtnType(bool btype)
    {
        receptBtn.SetActive(btype);
        callBtn.SetActive(!btype);
        completeBtn.SetActive(!btype);
    }

    public void ChangeOrderBoxColor(int index, bool is_selected)
    {
        if (index == -1 || Global.notcompleted_orderlist.Count <= index)
        {
            Debug.Log("Item does not exist");
            return;
        }
        Debug.Log(index + " status = " + Global.notcompleted_orderlist[index].status);
        if (!is_selected)
        {
            if (Global.notcompleted_orderlist[index].status == 1)
            {
                itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_new");
            }
            else if (Global.notcompleted_orderlist[index].status == 2)
            {
                if ((DateTime.Now - Convert.ToDateTime(Global.notcompleted_orderlist[index].order_time)).TotalSeconds > Global.setInfo.cooking_time * 60)
                {
                    itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking1");
                }
                else
                {
                    itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking2");
                    StartCoroutine(UpdateBoxColor(itemObj[index]));
                }
            }
            else
            {
                itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_complete");
            }
        }
        else
        {
            if (Global.notcompleted_orderlist[index].status == 1)
            {
                itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_new_selected");
                try
                {
                    ShowBtnType(true);
                    rebackBtn.SetActive(false);
                }
                catch (Exception ex)
                {

                }
            }
            else if (Global.notcompleted_orderlist[index].status == 2)
            {
                if ((DateTime.Now - Convert.ToDateTime(Global.notcompleted_orderlist[index].order_time)).TotalSeconds > Global.setInfo.cooking_time * 60)
                {
                    itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking1_selected");
                }
                else
                {
                    itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_cooking2_selected");
                    StartCoroutine(UpdateBoxColor(itemObj[index]));
                }
                try
                {
                    ShowBtnType(false);
                    rebackBtn.SetActive(true);
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                itemObj[index].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_complete_selected");
                try
                {
                    ShowBtnType(true);
                    receptBtn.SetActive(false);
                    rebackBtn.SetActive(true);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }

    IEnumerator ShowMenulist(int i)
    {
        //clear
        while (menu_itemParent.transform.childCount > 0)
        {
            try
            {
                DestroyImmediate(menu_itemParent.transform.GetChild(0).gameObject);
            }
            catch (Exception ex)
            {

            }
            yield return new WaitForSeconds(0.01f);
        }
        current_time.text = Global.GetCookTimeFormat(Convert.ToInt32((DateTime.Now - Convert.ToDateTime(Global.notcompleted_orderlist[i].order_time)).TotalMinutes)) + "분 경과";
        if (Global.notcompleted_orderlist[i].is_tableChanged != 1)
        {
            menuitemObj = null;
            menuitemObj = new GameObject[Global.notcompleted_orderlist[i].menulist.Count];
            int is_c = Global.notcompleted_orderlist[i].is_cancel;
            if (is_c == 1)
            {
                cancelNotice.SetActive(true);
                rebackBtn.SetActive(false);
                receptBtn.gameObject.SetActive(true);
                callBtn.gameObject.SetActive(false);
                completeBtn.gameObject.SetActive(false);
            }
            else
            {
                cancelNotice.SetActive(false);
                if (Global.notcompleted_orderlist[i].status != 1)
                {
                    rebackBtn.SetActive(true);
                }
            }
            for (int j = 0; j < Global.notcompleted_orderlist[i].menulist.Count; j++)
            {
                menuitemObj[j] = Instantiate(menu_item);
                menuitemObj[j].transform.SetParent(menu_itemParent.transform);

                int _i = i;
                int _j = j;
                menuitemObj[j].GetComponent<Button>().onClick.RemoveAllListeners();
                menuitemObj[j].GetComponent<Button>().onClick.AddListener(delegate () { ProcessItem(_i, _j); });

                try
                {
                    menuitemObj[j].transform.Find("name").GetComponent<Text>().text = Global.notcompleted_orderlist[i].menulist[j].name;
                    if (is_c == 1)
                    {
                        menuitemObj[j].transform.Find("amount").GetComponent<Text>().text = "-" + Global.notcompleted_orderlist[i].menulist[j].amount.ToString();
                        menuitemObj[j].transform.Find("status").GetComponent<Image>().sprite = null;
                    }
                    else
                    {
                        menuitemObj[j].transform.Find("amount").GetComponent<Text>().text = Global.notcompleted_orderlist[i].menulist[j].amount.ToString();
                        if (Global.notcompleted_orderlist[i].menulist[j].status == 3)
                        {
                            menuitemObj[j].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("check");
                        }
                        else
                        {
                            menuitemObj[j].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("check1");
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                menuitemObj[j].transform.localPosition = Vector3.zero;
                if (menu_itemParent.transform.childCount > 1)
                {
                    menuitemObj[j].transform.GetComponent<RectTransform>().sizeDelta =
                        new Vector2(menu_itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.x,
                        menu_itemParent.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta.y);
                }
                else
                {
                    menuitemObj[j].transform.GetComponent<RectTransform>().sizeDelta =
                        new Vector2(-menu_itemParent.transform.GetComponent<RectTransform>().sizeDelta.x,
                        -menu_itemParent.transform.GetComponent<RectTransform>().sizeDelta.y / 6);
                }
                menuitemObj[j].transform.localScale = Vector3.one;
            }
        }
        //is_loading = true;
        old_i = i;
        curOrderSeq = Global.notcompleted_orderlist[i].orderNo;
        curTagName = Global.notcompleted_orderlist[i].tagName;
        Debug.Log("cur tag name = " + curTagName);
        curTableName = Global.notcompleted_orderlist[i].tableNo;
        Debug.Log("curOrderSeq = " + curOrderSeq);
    }

    public void SelectItem(int i)
    {
        Debug.Log("select item = " + i);
        if (i == -1 || Global.notcompleted_orderlist.Count <= i)
        {
            Debug.Log("Item not exist");
            ShowMenuPanel(false);
            receptBtn.SetActive(false);
            completeBtn.SetActive(false);
            callBtn.SetActive(false);
            return;
        }
        //if (Global.notcompleted_orderlist[i].status == 3)
        //    return;

        if (Global.notcompleted_orderlist[i].is_tableChanged == 1)
        {
            ShowPanelType(true);
            if (old_i != -1)
            {
                ChangeOrderBoxColor(old_i, false);
            }
            itemObj[i].transform.Find("back").GetComponent<Image>().sprite = Resources.Load<Sprite>("rect_new_selected");
            table_no.text = "";
            order_no.text = "";
            tablePanel.transform.Find("tablename").GetComponent<Text>().text = Global.notcompleted_orderlist[i].tableNo;
            tablePanel.transform.Find("old_tablename").GetComponent<Text>().text = Global.notcompleted_orderlist[i].old_tableNo;
            //is_loading = true;
            old_i = i;
            curOrderSeq = Global.notcompleted_orderlist[i].orderNo;
            Debug.Log("curOrderSeq = " + curOrderSeq);
        }
        else
        {
            if (i == old_i || Global.notcompleted_orderlist.Count == 0)
                return;
            ShowPanelType(false);
            order_time.text = Global.GetOrderTimeFormat(Global.notcompleted_orderlist[i].order_time) + " 주문";
            table_no.text = Global.notcompleted_orderlist[i].tableNo;
            order_no.text = Global.GetONoFormat(Global.notcompleted_orderlist[i].orderNo);
            order_price.text = Global.GetPriceFormat(Global.notcompleted_orderlist[i].price);
            if (Global.notcompleted_orderlist[i].order_type == 1)
            {
                pozhangNotice.SetActive(true);
            }
            else
            {
                pozhangNotice.SetActive(false);
            }
            if (old_i != -1)
            {
                ChangeOrderBoxColor(old_i, false);
            }
            ChangeOrderBoxColor(i, true);
            StartCoroutine(ShowMenulist(i));
        }
    }

    public void onBack()
    {
        curSceneType = SceneType.main;
        Global.cur_orderNo = -1;
        ShowScene();
    }

    public void GoSetting()
    {
        curSceneType = SceneType.setting;
        ShowScene();
    }

    IEnumerator ProcessRebackOrder(WWW www)
    {
        yield return www;
        is_send_request = false;
        if (www.error == null)
        {
            Debug.Log("reback success!");
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                if (curSceneType == SceneType.main)
                {
                    Global.new_order_cnt++;
                    Global.cooking_cnt--;
                    cooking_Cnt_Txt.text = Global.cooking_cnt.ToString();
                    new_order_Cnt_Txt.text = Global.new_order_cnt.ToString();
                    OrderItem oinfo = Global.notcompleted_orderlist[old_i];
                    Global.ChangeOrderInfo(old_i, oinfo, 1);
                    ChangeOrderBoxColor(old_i, true);
                    for (int i = 0; i < menuitemObj.Length; i++)
                    {
                        try
                        {
                            menuitemObj[i].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("check1");
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                else
                {
                    //Global.orderlist 변경
                    OrderItem oinfo = Global.completed_orderlist[old_i];
                    oinfo.status = 2;   //주문
                    for (int j = 0; j < oinfo.menulist.Count; j++)
                    {
                        MenuOrderInfo minfo = oinfo.menulist[j];
                        minfo.status = 2;
                        oinfo.menulist[j] = minfo;
                    }
                    Global.completed_orderlist.Remove(Global.completed_orderlist[old_i]);
                    Global.notcompleted_orderlist.Add(oinfo);

                    Global.cooking_cnt++;
                    Global.completion_cnt--;
                    Debug.Log("completion cnt --");
                    cooking_Cnt_Txt.text = Global.cooking_cnt.ToString();
                    complete_Cnt_Txt.text = Global.completion_cnt.ToString();
                    curSceneType = SceneType.main;
                    ShowScene();
                }
            }
            else
            {
                ShowMessage(jsonNode["msg"]);
            }
        }
        else
        {
            ShowMessage("주문되돌리기에 실패하었습니다.");
        }
    }

    public void Reback()
    {
        //되돌리기
        if (is_send_request)
        {
            return;
        }
        if (curSceneType == SceneType.main)
        {
            Debug.Log(Global.notcompleted_orderlist[old_i].orderNo + "," + curOrderSeq);
            if (Global.notcompleted_orderlist.Count == 0)
            {
                return;
            }
            if (Global.notcompleted_orderlist[old_i].orderNo == curOrderSeq)
            {
                Debug.Log("되돌리기 " + Global.notcompleted_orderlist[old_i].status);
                if (Global.notcompleted_orderlist[old_i].status <= 1)
                    return;
                //개별적인 메뉴들의 주문상태 되돌리기 api
                WWWForm form = new WWWForm();
                form.AddField("orderSeq", curOrderSeq);
                if(curTagName != null && curTagName != "")
                {
                    form.AddField("tagName", curTagName);
                }
                form.AddField("tableName", curTableName);
                int status = Global.notcompleted_orderlist[old_i].status;
                if (status > 1)
                {
                    status--;
                }
                form.AddField("status", status);
                WWW www = new WWW(Global.api_url + Global.reback_order_api, form);
                StartCoroutine(ProcessRebackOrder(www));
                is_send_request = true;
            }
        }
        else if (curSceneType == SceneType.completed)
        {
            if (Global.completed_orderlist.Count == 0)
            {
                return;
            }
            if (Global.completed_orderlist[old_i].is_cancel == 1)
            {
                return;
            }
            Debug.Log(Global.completed_orderlist[old_i].orderNo + "," + curOrderSeq);
            if (Global.completed_orderlist[old_i].orderNo == curOrderSeq)
            {
                Debug.Log("되돌리기 " + Global.completed_orderlist[old_i].status);
                if (Global.completed_orderlist[old_i].status <= 1)
                    return;
                //개별적인 메뉴들의 주문상태 되돌리기 api
                WWWForm form = new WWWForm();
                form.AddField("orderSeq", curOrderSeq);
                if (curTagName != null && curTagName != "")
                {
                    form.AddField("tagName", curTagName);
                }
                int status = Global.completed_orderlist[old_i].status;
                if (status > 1)
                {
                    status--;
                }
                form.AddField("status", status);
                WWW www = new WWW(Global.api_url + Global.reback_order_api, form);
                StartCoroutine(ProcessRebackOrder(www));
                is_send_request = true;
            }
        }
    }

    IEnumerator ProcessCheckOrderTableChanged(WWW www)
    {
        yield return www;
        is_send_request = false;
        Debug.Log("set send_reqeust false");
        if (www.error == null)
        {
            //테이블변경주문인 경우 바로 완료처리후 박스 삭제
            Global.notcompleted_orderlist.Remove(Global.notcompleted_orderlist[old_i]);
            DestroyImmediate(itemObj[old_i].gameObject);
            itemObj.Remove(itemObj[old_i]);
            SelectItem(0);
            ChangeOrderBoxColor(0, true);
        }
    }

    IEnumerator ProcessCheckOrderCancel(WWW www)
    {
        yield return www;
        is_send_request = false;
        Debug.Log("set send_reqeust false");
        if (www.error == null)
        {
            //취소주문인 경우 완료처리후 완료리스트로.
            Global.completed_orderlist.Add(Global.notcompleted_orderlist[old_i]);
            Global.notcompleted_orderlist.Remove(Global.notcompleted_orderlist[old_i]);
            DestroyImmediate(itemObj[old_i].gameObject);
            itemObj.Remove(itemObj[old_i]);
            SelectItem(0);
            ChangeOrderBoxColor(0, true);
        }
    }

    public void Recept()
    {
        if (Global.notcompleted_orderlist.Count == 0 || is_send_request)
        {
            return;
        }
        Debug.Log(is_send_request);
        if (Global.notcompleted_orderlist[old_i].is_tableChanged == 1)
        {
            //api 통해서 통지
            WWWForm form = new WWWForm();
            Debug.Log("Recept : ProcessCheckOrderTableChanged");
            Debug.Log(old_i + "," + Global.notcompleted_orderlist[old_i].id);
            form.AddField("id", Global.notcompleted_orderlist[old_i].id);
            WWW www = new WWW(Global.api_url + Global.is_check_recept_neworder, form);
            StartCoroutine(ProcessCheckOrderTableChanged(www));
        }
        else
        {
            Debug.Log("old_i = " + old_i + "," + Global.notcompleted_orderlist[old_i].order_type);
            if (Global.notcompleted_orderlist[old_i].is_cancel == 1)
            {
                Debug.Log("Recept : ProcessCheckOrderCancel");
                //api 통해서 통지
                WWWForm form = new WWWForm();
                form.AddField("id", Global.notcompleted_orderlist[old_i].id);
                WWW www = new WWW(Global.api_url + Global.is_check_recept_neworder, form);
                StartCoroutine(ProcessCheckOrderCancel(www));
            }
            else
            {
                Debug.Log("Recept : ProcessReceptOrder");
                //주문접수 api
                WWWForm form = new WWWForm();
                form.AddField("orderSeq", curOrderSeq);
                if (curTagName != null && curTagName != "")
                {
                    form.AddField("tagName", curTagName);
                }
                form.AddField("tableName", curTableName);
                WWW www = new WWW(Global.api_url + Global.recept_api, form);
                StartCoroutine(ProcessReceptOrder(www));
            }
        }
        is_send_request = true;
        Debug.Log("set send_reqeust true");
    }

    IEnumerator ProcessReceptOrder(WWW www)
    {
        yield return www;
        is_send_request = false;
        Debug.Log("set send_reqeust false");
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                Debug.Log("주문접수 성공.");
                Global.ChangeOrderInfo(old_i, Global.notcompleted_orderlist[old_i], 2);
                Debug.Log("변수 상태 변경 = " + Global.notcompleted_orderlist[old_i].status);
                Global.new_order_cnt--;
                new_order_Cnt_Txt.text = Global.new_order_cnt.ToString();
                Global.cooking_cnt++;
                cooking_Cnt_Txt.text = Global.cooking_cnt.ToString();
                SelectItem(old_i);
                ChangeOrderBoxColor(old_i, true);
            }
            else
            {
                ShowMessage(jsonNode["msg"]);
            }
        }
        else
        {
            ShowMessage("주문접수에 실패하었습니다.");
        }
    }

    public void Call()
    {
        //호출
        if (is_send_request)
            return;
        WWWForm form = new WWWForm();
        form.AddField("orderSeq", curOrderSeq);
        if (curTagName != null && curTagName != "")
        {
            form.AddField("tagName", curTagName);
        }
        form.AddField("tableName", curTableName);
        form.AddField("udid", Global.udid);
        Debug.Log("udid = " + Global.udid);
        WWW www = new WWW(Global.api_url + Global.call_api, form);
        StartCoroutine(ProcessCallOrder(www));
        is_send_request = true;
        Debug.Log("set send_reqeust true");
    }

    public void Complete()
    {
        Debug.Log("old_i" + old_i + ": is_send_request = " + is_send_request);
        if (old_i == -1 || is_send_request) return;
        //완료
        WWWForm form = new WWWForm();
        form.AddField("orderSeq", curOrderSeq);
        if (curTagName != null && curTagName != "")
        {
            form.AddField("tagName", curTagName);
        }
        form.AddField("tableName", curTableName);
        WWW www = new WWW(Global.api_url + Global.complete_api, form);
        StartCoroutine(ProcessCompleteOrder(www));
        is_send_request = true;
        Debug.Log("set send_reqeust true");
    }

    IEnumerator ProcessCallOrder(WWW www)
    {
        yield return www;
        is_send_request = false;
        Debug.Log("set send_reqeust false");
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                OrderItem oitem = Global.notcompleted_orderlist[old_i];
                oitem.status = 3;
                oitem.is_call = true;
                for (int i = 0; i < Global.notcompleted_orderlist[old_i].menulist.Count; i++)
                {
                    MenuOrderInfo minfo = Global.notcompleted_orderlist[old_i].menulist[i];
                    minfo.status = 3;
                    oitem.menulist[i] = minfo;
                }
                Global.notcompleted_orderlist[old_i] = oitem;
                SelectItem(old_i);
                ChangeOrderBoxColor(old_i, true);

                oitem.is_call = false;
                Global.completed_orderlist.Add(oitem);
                Global.cooking_cnt--;
                cooking_Cnt_Txt.text = Global.cooking_cnt.ToString();
                Global.completion_cnt++;
                Debug.Log("completion cnt ++");
                complete_Cnt_Txt.text = Global.completion_cnt.ToString();
                yield return new WaitForSeconds(30f);
                Debug.Log("30 seconds flowed!");
                Global.notcompleted_orderlist.Remove(Global.notcompleted_orderlist[old_i]);
                DestroyImmediate(itemObj[old_i].gameObject);
                itemObj.Remove(itemObj[old_i]);
                for (int i = old_i; i < itemObj.Count; i++)
                {
                    itemObj[i].GetComponent<Button>().onClick.RemoveAllListeners();
                    int _i = i;
                    itemObj[i].GetComponent<Button>().onClick.AddListener(delegate () { SelectItem(_i); });
                }
                old_i = -1;
                Debug.Log("set old_i = -1");

                if (Global.notcompleted_orderlist.Count > 0)
                {
                    SelectItem(0);
                    ChangeOrderBoxColor(0, true);
                }
                else
                {
                    ShowMenuPanel(false);
                    receptBtn.SetActive(false);
                    completeBtn.SetActive(false);
                    callBtn.SetActive(false);
                }
            }
            else
            {
                ShowMessage(jsonNode["msg"]);
            }
        }
        else
        {
            ShowMessage("주문호출에 실패하었습니다.");
        }
    }

    IEnumerator ProcessCompleteOrder(WWW www)
    {
        yield return www;
        is_send_request = false;
        Debug.Log("set send_reqeust false");
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                OrderItem oitem = Global.notcompleted_orderlist[old_i];
                for (int i = 0; i < Global.notcompleted_orderlist[old_i].menulist.Count; i++)
                {
                    MenuOrderInfo minfo = Global.notcompleted_orderlist[old_i].menulist[i];
                    minfo.status = 3;
                    oitem.menulist[i] = minfo;
                }
                Global.completed_orderlist.Add(oitem);
                Global.notcompleted_orderlist.Remove(Global.notcompleted_orderlist[old_i]);
                DestroyImmediate(itemObj[old_i].gameObject);
                itemObj.Remove(itemObj[old_i]);
                Global.cooking_cnt--;
                cooking_Cnt_Txt.text = Global.cooking_cnt.ToString();
                Global.completion_cnt++;
                Debug.Log("completion cnt ++");
                complete_Cnt_Txt.text = Global.completion_cnt.ToString();
                for (int i = old_i; i < itemObj.Count; i++)
                {
                    itemObj[i].GetComponent<Button>().onClick.RemoveAllListeners();
                    int _i = i;
                    itemObj[i].GetComponent<Button>().onClick.AddListener(delegate () { SelectItem(_i); });
                }
                old_i = -1;
                Debug.Log("set old_i = -1");

                if (Global.notcompleted_orderlist.Count > 0)
                {
                    SelectItem(0);
                    ChangeOrderBoxColor(0, true);
                }
                else
                {
                    ShowMenuPanel(false);
                    receptBtn.SetActive(false);
                    completeBtn.SetActive(false);
                    callBtn.SetActive(false);
                }
            }
            else
            {
                ShowMessage(jsonNode["msg"]);
            }
        }
        else
        {
            ShowMessage("주문완료에 실패하었습니다.");
        }
    }

    public void ShowCompletedOrderlist()
    {
        curSceneType = SceneType.completed;
        ShowScene();
    }

    IEnumerator ProcessCompleteMenu(WWW www, int oNo, int mNo)
    {
        yield return www;
        is_send_request = false;
        Debug.Log("set send_reqeust false");
        if (www.error == null)
        {
            Debug.Log("개별메뉴 주문완료 성공!");
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                Global.ChangeMenuorderInfo(oNo, mNo, 3);
                Debug.Log("개별메뉴완료 메뉴=" + Global.notcompleted_orderlist[oNo].menulist[mNo].status);
                //UI 상태체크 변경
                try
                {
                    menuitemObj[mNo].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("check");
                }
                catch (Exception ex)
                {

                }
                CheckOrderUpdate();
            }
            else
            {
                ShowMessage(jsonNode["msg"]);
            }
        }
        else
        {
            ShowMessage("메뉴주문 완료에 실패하었습니다.");
        }
    }

    void ShowMessage(string content)
    {
        err_msg.text = content;
        error_popup.SetActive(true);
    }

    private void CompleteMenu(int oNo, int mNo)
    {
        Debug.Log(oNo + " order, " + mNo + " menu complete process." + Global.notcompleted_orderlist[oNo].status);
        Debug.Log("is send request = " + is_send_request);
        if (is_send_request)
            return;
        //메뉴 개별완료처리
        Debug.Log("개별메뉴완료 = " + Global.notcompleted_orderlist[oNo].status);
        if (Global.notcompleted_orderlist[oNo].menulist[mNo].status != 2)
            return;
        WWWForm form = new WWWForm();
        form.AddField("order_id", Global.notcompleted_orderlist[oNo].menulist[mNo].order_id);
        Debug.Log("개별메뉴완료 태그명=" + Global.notcompleted_orderlist[oNo].tagName);
        form.AddField("tagName", Global.notcompleted_orderlist[oNo].tagName);
        form.AddField("tableName", Global.notcompleted_orderlist[oNo].tableNo);
        form.AddField("status", 3);
        WWW www = new WWW(Global.api_url + Global.set_order_api, form);
        StartCoroutine(ProcessCompleteMenu(www, oNo, mNo));
        is_send_request = true;
        Debug.Log("set send_reqeust true");
    }

    IEnumerator ProcessCancelCompleteMenu(WWW www, int oNo, int mNo)
    {
        yield return www;
        is_send_request = false;
        Debug.Log("set send_reqeust false");
        if (www.error == null)
        {
            Debug.Log("개별메뉴 완료취소 성공!");
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                Global.ChangeMenuorderInfo(oNo, mNo, 2);
                //UI 상태체크 변경
                try
                {
                    menuitemObj[mNo].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("check1");
                }
                catch (Exception ex)
                {

                }
                CheckOrderUpdate();
            }
            else
            {
                ShowMessage(jsonNode["msg"]);
            }
        }
        else
        {
            ShowMessage("메뉴주문 완료취소에 실패하었습니다.");
        }
    }

    private void CancelCompletedMenu(int oNo, int mNo)
    {
        //완료취소처리
        Debug.Log("완료취소처리 " + oNo + ", " + mNo);
        Debug.Log(is_send_request);
        if (Global.notcompleted_orderlist[oNo].menulist[mNo].status != 3 || is_send_request)
            return;
        WWWForm form = new WWWForm();
        form.AddField("order_id", Global.notcompleted_orderlist[oNo].menulist[mNo].order_id);
        form.AddField("status", 2);
        form.AddField("tagName", Global.notcompleted_orderlist[oNo].tagName);
        form.AddField("tableName", Global.notcompleted_orderlist[oNo].tableNo);
        WWW www = new WWW(Global.api_url + Global.set_order_api, form);
        StartCoroutine(ProcessCancelCompleteMenu(www, oNo, mNo));
        is_send_request = true;
        Debug.Log("set send_reqeust true");
    }

    public void onConfirmErrPopup()
    {
        error_popup.SetActive(false);
    }

    void CheckOrderUpdate()
    {
        bool not_changed = false;
        int status = 0;
        Debug.Log("old i = " + old_i);
        if (Global.notcompleted_orderlist[old_i].menulist.Count > 0)
        {
            status = Global.notcompleted_orderlist[old_i].menulist[0].status;
        }
        for (int i = 0; i < Global.notcompleted_orderlist[old_i].menulist.Count; i++)
        {
            Debug.Log("s1 = " + status + ", s2=" + Global.notcompleted_orderlist[old_i].menulist[i].status);
            if (Global.notcompleted_orderlist[old_i].menulist[i].status != status)
            {
                not_changed = true; break;
            }
        }
        if (!not_changed)
        {
            Global.ChangeOrderInfo(old_i, Global.notcompleted_orderlist[old_i], status);
            if (status == 3)
            {
                Global.completed_orderlist.Add(Global.notcompleted_orderlist[old_i]);
                Global.notcompleted_orderlist.Remove(Global.notcompleted_orderlist[old_i]);
                DestroyImmediate(itemObj[old_i].gameObject);
                itemObj.Remove(itemObj[old_i]);
                Global.cooking_cnt--;
                cooking_Cnt_Txt.text = Global.cooking_cnt.ToString();
                Global.completion_cnt++;
                Debug.Log("completion cnt ++");
                complete_Cnt_Txt.text = Global.completion_cnt.ToString();
                for (int i = old_i; i < itemObj.Count; i++)
                {
                    itemObj[i].GetComponent<Button>().onClick.RemoveAllListeners();
                    int _i = i;
                    itemObj[i].GetComponent<Button>().onClick.AddListener(delegate () { SelectItem(_i); });
                }
                old_i = -1;
                Debug.Log("set old_i = -1");

                if (Global.notcompleted_orderlist.Count > 0)
                {
                    SelectItem(0);
                    ChangeOrderBoxColor(0, true);
                }
                else
                {
                    ShowMenuPanel(false);
                    receptBtn.SetActive(false);
                    completeBtn.SetActive(false);
                    callBtn.SetActive(false);
                }
            }
            else
            {
                Debug.Log("!!!!!!!!!");
                ChangeOrderBoxColor(old_i, true);
            }
        }
    }

    public void socketOpen(SocketIOEvent e)
    {
    }

    public void CallClient()
    {
        //if(Global.setInfo.call_type == 1)
        //{
        //monitor
        string str = "{\"is_client_call_type\":1}";
        Debug.Log("json string = " + JSONObject.Create(str));
        socket.Emit("CallClientFromKit", JSONObject.Create(str));
        //}
    }

    public void GetUDIDFromServer(SocketIOEvent e)
    {
        Debug.Log("Get udid from the server.");
        Debug.Log("home socket open -- ");
        if (is_socket_open)
            return;
        is_socket_open = true;
        if (!Global.is_found_udid)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            Global.udid = jsonNode["udid"];
            Global.is_found_udid = true;
        }
        Debug.Log("udid = " + Global.udid);
        string busid = "{\"call_type\":\"" + Global.setInfo.call_type + "\",\"udid\":\"" + Global.udid + "\"}";
        Debug.Log("json string = " + JSONObject.Create(busid));
        socket.Emit("kitSetInfo", JSONObject.Create(busid));
    }

    public void CreateOrerEventProcess(SocketIOEvent e)
    {
        Debug.Log("create order socket event." + e.data);
        StartCoroutine(ProcessCreateOrderEvent(JSON.Parse(e.data.ToString())));
    }

    IEnumerator ProcessCreateOrderEvent(JSONNode data)
    {
        Global.cur_orderNo = old_i;
        GameObject.Find("Audio Source").GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(1f);
        Debug.Log(data);
        bool isMenuExist = false;
        OrderItem oitem = new OrderItem();
        oitem.orderNo = data["orderSeq"].AsInt;
        oitem.tagName = data["tagName"];
        oitem.is_call = false;
        oitem.is_tableChanged = data["is_tableChanged"].AsInt;
        oitem.status = 1;//kit_stat
        if (oitem.is_tableChanged == 1)
        {
            oitem.tableNo = data["new_tablename"];
            oitem.old_tableNo = data["old_tablename"];
            oitem.id = data["id"].AsInt;

            isMenuExist = true;
        }
        else
        {
            oitem.tableNo = data["tableName"];
            oitem.order_type = data["is_pack"].AsInt;
            oitem.is_cancel = data["is_cancel"].AsInt;//1-취소주문
            if (oitem.is_cancel == 1)
            {
                oitem.id = data["id"].AsInt;
            }
            oitem.price = data["price"].AsInt;
            oitem.order_time = data["reg_datetime"];

            JSONNode m_list = JSON.Parse(data["menulist"].ToString());
            oitem.menulist = new List<MenuOrderInfo>();
            for (int j = 0; j < m_list.Count; j++)
            {
                if(m_list[j]["is_kitchen"].AsInt == 1)
                {
                    MenuOrderInfo minfo = new MenuOrderInfo();
                    minfo.order_id = m_list[j]["order_id"];
                    minfo.menu_id = m_list[j]["product_id"];
                    minfo.name = m_list[j]["product_name"];
                    minfo.amount = m_list[j]["quantity"].AsInt;
                    minfo.status = 1;
                    oitem.menulist.Add(minfo);

                    isMenuExist = true;
                }
            }
        }
        if(isMenuExist)
        {
            Global.new_order_cnt++;
            Global.notcompleted_orderlist.Add(oitem);
            ShowScene();
        }
    }

    public void CallClientProcess(SocketIOEvent e)
    {
        Debug.Log("call client socket event.");
        if (Global.setInfo.call_recept_option == 0)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            call_txt.text = jsonNode["tableNo"];
            call_popup.SetActive(true);
            GameObject.Find("Audio Source").GetComponent<AudioSource>().Play();
        }
    }

    void Refresh(int type = 0)
    {
        is_send_request = false;
        StopAllCoroutines();
        LoadOrderInfo();
        ShowScene();
    }

    public void RefreshEvent(SocketIOEvent e)
    {
        Debug.Log("Refresh Event");
        Refresh();
    }

    public void socketError(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    public void socketClose(SocketIOEvent e)
    {
        is_socket_open = false;
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
    }

    public void OnApplicationQuit()
    {
        socket.Close();
        socket.OnDestroy();
        socket.OnApplicationQuit();
    }

    public void ProcessItem(int oNo, int mNo)
    {
        if (Global.notcompleted_orderlist[oNo].is_call)
            return;
        if (Global.notcompleted_orderlist[oNo].menulist[mNo].status == 2)
        {
            CompleteMenu(oNo, mNo);
        }
        else
        {
            CancelCompletedMenu(oNo, mNo);
        }
    }

    public void onNewOrder()
    {
        Refresh();
    }

    //dragEvent draggable = menuitemObj[j].transform.GetComponent<dragEvent>();
    //draggable.oNo = i;
    //draggable.mNo = j;
    //draggable.OnEndDragCallback += EndDragCallback;

    //EventTrigger trigger = menuitemObj[j].GetComponent<EventTrigger>();
    //EventTrigger.Entry entry = new EventTrigger.Entry();
    //entry.eventID = EventTriggerType.EndDrag;
    //int _i = i;
    //int _j = j;
    //entry.callback.AddListener((data) => { OnEndDrag((PointerEventData)data, _i, _j); });
    //trigger.triggers.Add(entry);

    //public void EndDragCallback(PointerEventData eventData, int oNo, int mNo)
    //{
    //    Debug.Log("oNo = " + oNo + ", mNo = " + mNo);
    //    if (Global.notcompleted_orderlist[oNo].status == 1)
    //    {
    //        Debug.Log("return pending status.");
    //        return;
    //    }
    //    Vector3 dragVectorDirection = (eventData.position - eventData.pressPosition).normalized;
    //    ProcessByDirection(dragVectorDirection, oNo, mNo);
    //}

    //private enum DraggedDirection
    //{
    //    Up,
    //    Down,
    //    Right,
    //    Left
    //}

    //private void ProcessByDirection(Vector3 dragVector, int oNo, int mNo)
    //{
    //    float positiveX = Mathf.Abs(dragVector.x);
    //    float positiveY = Mathf.Abs(dragVector.y);
    //    if (positiveX > positiveY)
    //    {
    //        if (dragVector.x > 0)
    //        {
    //            Debug.Log("right direction.");
    //            CompleteMenu(oNo, mNo);
    //        }
    //        else
    //        {
    //            Debug.Log("left direction.");
    //            CancelCompletedMenu(oNo, mNo);
    //        }
    //    }
    //    //else
    //    //{
    //    //    draggedDir = (dragVector.y > 0) ? DraggedDirection.Up : DraggedDirection.Down;
    //    //}
    //}
    //private Vector2 fingerDown;
    //private Vector2 fingerUp;
    //public bool detectSwipeOnlyAfterRelease = false;
    //public float SWIPE_THRESHOLD = 20f;

    //void Update()
    //{
    //    foreach (Touch touch in Input.touches)
    //    {
    //        Debug.Log("touching--");
    //        if (touch.phase == TouchPhase.Began)
    //        {
    //            fingerUp = touch.position;
    //            fingerDown = touch.position;
    //        }

    //        //Detects Swipe while finger is still moving
    //        if (touch.phase == TouchPhase.Moved)
    //        {
    //            if (!detectSwipeOnlyAfterRelease)
    //            {
    //                fingerDown = touch.position;
    //                checkSwipe();
    //            }
    //        }

    //        //Detects swipe after finger is released
    //        if (touch.phase == TouchPhase.Ended)
    //        {
    //            fingerDown = touch.position;
    //            checkSwipe();
    //        }
    //    }
    //}

    //void checkSwipe()
    //{
    //    //Check if Vertical swipe
    //    if (verticalMove() > SWIPE_THRESHOLD && verticalMove() > horizontalValMove())
    //    {
    //        //Debug.Log("Vertical");
    //        if (fingerDown.y - fingerUp.y > 0)//up swipe
    //        {
    //            OnSwipeUp();
    //        }
    //        else if (fingerDown.y - fingerUp.y < 0)//Down swipe
    //        {
    //            OnSwipeDown();
    //        }
    //        fingerUp = fingerDown;
    //    }

    //    //Check if Horizontal swipe
    //    else if (horizontalValMove() > SWIPE_THRESHOLD && horizontalValMove() > verticalMove())
    //    {
    //        //Debug.Log("Horizontal");
    //        if (fingerDown.x - fingerUp.x > 0)//Right swipe
    //        {
    //            OnSwipeRight();
    //        }
    //        else if (fingerDown.x - fingerUp.x < 0)//Left swipe
    //        {
    //            OnSwipeLeft();
    //        }
    //        fingerUp = fingerDown;
    //    }

    //    //No Movement at-all
    //    else
    //    {
    //        //Debug.Log("No Swipe!");
    //    }
    //}

    //float verticalMove()
    //{
    //    return Mathf.Abs(fingerDown.y - fingerUp.y);
    //}

    //float horizontalValMove()
    //{
    //    return Mathf.Abs(fingerDown.x - fingerUp.x);
    //}

    //void OnSwipeUp()
    //{
    //    Debug.Log("Swipe UP");
    //}

    //void OnSwipeDown()
    //{
    //    Debug.Log("Swipe Down");
    //}

    //void OnSwipeLeft()
    //{
    //    Debug.Log("Swipe Left");
    //}

    //void OnSwipeRight()
    //{
    //    Debug.Log("Swipe Right");
    //}


    //socket io
}