using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using YKF;
using static AutoBuy.AutoBuy;
using static BGMData;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.UI.CanvasScaler;
using Text = UnityEngine.UI.Text;
using UDebug = UnityEngine.Debug;
namespace AutoBuy
{
    public class ConfigLayer : YKLayer<object>
    {
        public Button closeButton;
        public CustomTab settingTab;
        public override Rect Bound { get; } = new Rect(0, 0, 700, 700);
        public override void OnLayout()
        {
             settingTab =  CreateTab<CustomTab>("AuotoBuySetting", "setting1");
        }
        public static void CreateLayer()
        {
           ConfigLayer layer = YK.CreateLayer<ConfigLayer>();
            AutoBuy.layer = layer;
            return ;
        }

    }
    public class CustomTab : YKLayout<object>
    {
        public List<string> filterList = new List<string> { "Default", "Name", "Detail", "Tags", "Element" , "StockNum" };
        public const float textWidth = 110;
        public const float valueWidth = 150;
        public const int planCount = 100;
        public List<PlanLayout> planeList = new List<PlanLayout>();
        public GameObject addButtonObject;
        public ScrollRect scrollRect;
        public override void OnLayout()
        {
            Init();
            YKLayout mainLayout = this;
            RerollNumItem(mainLayout);
            IsGuideItem(mainLayout);
            Header(mainLayout);
            StaticBuild();
        }
        public YKLayout RerollNumItem(YKLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            UIText text =ho.Text("RerollNum", color: FontColor.DontChange);
            SetSize(text, w: textWidth);
            string s = AutoBuy.configeData.rerollNum.ToString();
            var value = ho.InputText(s, onInput: (v) => { AutoBuy.configeData.rerollNum = v;});
            value.Find("Text").gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            SetSize(value, w: valueWidth);
            return ho;
        }
        public YKLayout IsGuideItem(YKLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            UIText text = ho.Text("IsGuide", color: FontColor.DontChange);
            SetSize(text, w: textWidth);
            ho.Toggle("", isOn: AutoBuy.configeData.isGuide, onClick: (b) => { AutoBuy.configeData.isGuide = b; });
            return ho;

        }
        public YKLayout Header(YKLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            ho.Header("IsActive");
            ho.Header("keyWord");
            ho.Spacer(1, 40);
            ho.Header("FilterMdoe");
            ho.Spacer(1, 40);
            ho.Header("IsAllMatch");
            YKVertical verticalButtonLayout = ho.Vertical();
            UIButton deleteAllButton = ho.Button("DeleteAll", () => { RemoveAll(); });
            SetSize(deleteAllButton, 50, 100);
            ho.Spacer(1, 30);
            return ho;
        }
        public YKLayout IsActiveItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            father.activeButton=ho.Toggle((father.id+1).ToString(), isOn: true, onClick:father.OnActiveButton);
            float offset = 20;
            Transform textTransform = father.activeButton.mainText.transform;
            Transform imageTransform = father.activeButton.transform.Find("Image");
            textTransform.position -= Vector3.right * offset*2;
            imageTransform.position+= Vector3.right * offset*1.2f;
            SetSize(father.activeButton, 36, 60);
            return ho;
        }
     public YKLayout keyWordItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            //var text = ho.Text("keyWord", color: FontColor.DontChange);
            //SetSize(text, w: textWidth);
            string s = "";
            father.keyWordInput = UIInputSet(s, layOut: ho,onInput:father.OnKeyWordInput);
            return ho;
        }
        public YKLayout FilterMdoeItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            //UIText text = ho.Text("FilterMdoe", color: FontColor.DontChange);
            //SetSize(text, w: textWidth);
            father.drop = ho.Dropdown(filterList, value: 0, action: father.OnDrop);
            SetSize(father.drop, w: valueWidth);
            return ho;
        }
        public YKLayout IsAllMatchItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            //UIText text =ho.Text("IsAllMatch", color: FontColor.DontChange);
            //SetSize(text, w: textWidth);
            father.allMatchButton = ho.Toggle("", isOn:false, onClick: father.OnAllMatchButton);
            SetSize(father.allMatchButton,36,60);
            return ho;

        }
        public YKLayout RemoveItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            ho.Button("Delete", () =>
            {
                Remove(father);
            });
            return ho;
        }
        public YKLayout PlanItem(YKLayout father,int id)
        {
            PlanLayout ho = CreatePlanLayout(father.GetComponent<Transform>());
            ho.SetId(id);

            IsActiveItem(ho);
            ho.Spacer(1, 35);
            keyWordItem(ho);
            ho.Spacer(1, 20);
            FilterMdoeItem(ho);
            ho.Spacer(1, 40);
            ho.allMatchObject = IsAllMatchItem(ho).gameObject;
            ho.spacerObject = ho.Spacer(1, 60).gameObject;
            ho.Spacer(1, 40);
            RemoveItem(ho);
            ho.RefreshState(ho.isShowIsAllMatch);
            planeList.Add(ho);
            return ho;
        }
        public void SetSize( Component com,float h = 36,float w =100)
        {
            com.Rect().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            com.GetOrCreate<LayoutElement>().preferredWidth = w;
        }    
        public InputField UIInputSet(string text, Action<string> onInput = null,YKLayout layOut = null)
        {
            
            Widget widget = Util.Instantiate<Widget>("UI/Widget/WidgetSearch", this)
              ?? Util.Instantiate<Widget>("UI/Widget/WidgetSearch/WidgetSearch", this);
            widget.gameObject.SetActive(false);
            Transform component = widget.transform.Find("Search Box");
            Transform d1 = widget.transform.Find("Search Box/ButtonGeneral");
            Transform d2 = widget.transform.Find("Search Box/ButtonGeneral (1)");
            component.SetParent(layOut.transform);
            widget.transform.DestroyObject();
            
            d1.DestroyObject();
            d2.DestroyObject();

            InputField input = component.Find("InputField").GetComponent<InputField>();
            
            if (onInput != null)
            {
                input.onValueChanged.AddListener(value => onInput?.Invoke(value));
            }
            input.text = text;
            input.gameObject.GetComponent<CanvasRenderer>().SetColor(new Color(0.8f, 0.8f, 0.8f, 0.2f));
            input.Find("Text").gameObject.GetComponent<Text>().color = new Color(0.28f, 0.22f, 0.13f, 1f);
            component.Rect().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40f);
            component.GetOrCreate<LayoutElement>().preferredWidth = 125f;
            return input;
        }
        public PlanLayout CreatePlanLayout(Transform fahter)
        {
            PlanLayout layout = YK.Create<PlanLayout>(fahter);
            layout.OnLayout();
            return layout;
        }
        public void AddPlan()
        {
            PlanData newData = new PlanData();
            AutoBuy.configeData.planList.Add(newData);
            RefreshList();
            /*AutoBuy.ReBuildUI();*/
            AutoBuy.layer.settingTab.scrollRect.normalizedPosition = Vector3.zero;
            
        }
        public void Remove(PlanLayout planLayout)
        {
            AutoBuy.configeData.planList.Remove(planLayout.data);
            /*AutoBuy.ReBuildUI();*/
            RefreshList();
        }
        public void RemoveAll()
        {
            AutoBuy.configeData.planList.Clear();
            /*AutoBuy.ReBuildUI();*/
            RefreshList();
        }
        public void StaticBuild()
        {
            for (int i = 0; i < planCount; i++)
            {
                PlanLayout planLayout = PlanItem(this, i) as PlanLayout;
                planLayout.gameObject.SetActive(false);
            }
            this.Spacer(5, 1);
            addButtonObject = this.Button("Add", () =>
            {
                AddPlan();
            }).gameObject;
            this.Spacer(70, 1);
            RefreshList();
        }
        public void Build()
        {
            for (int i = 0; i < AutoBuy.configeData.planList.Count; i++)
            {
                PlanLayout item = PlanItem(this, i) as PlanLayout;
                item.data = AutoBuy.configeData.planList[i];
                item.UIUpdata(item.data);

            }

            addButtonObject = this.Button("Add", () =>
            {
                AddPlan();
            }).gameObject;
            this.Spacer(20, 1);
        }
        public void RefreshList()
        {
            foreach (PlanLayout item in planeList)
            {
                item.gameObject.SetActive(false);
                item.data = null;
            }
            for(int i = 0; i < AutoBuy.configeData.planList.Count; i++)
            {
                if (i > planCount-1) {UDebug.Log("大于了列表设置上限"); break; }
                planeList[i].data = AutoBuy.configeData.planList[i];
                planeList[i].UIUpdata(AutoBuy.configeData.planList[i]);
                planeList[i].gameObject.SetActive(true);
            }
            RefreshAddButton();
        }
        public void RefreshAddButton()
        {
            if (AutoBuy.configeData.planList.Count >=planCount)
            {
                addButtonObject.SetActive(false);
            }
            else
            {
                addButtonObject.SetActive(true);
            }
        }
        public void GetScroll()
        {
            Transform t = transform.parent.parent.parent;
            scrollRect = t.GetComponent<ScrollRect>();
            //if (scrollRect != null) { UDebug.Log("成功"); }
        }
        public void Init()
        {
            GetScroll();
        }
    }
    public class ConfigData
    {
        public bool isGuide = true;
        public int rerollNum = 0;
        public List<PlanData> planList = new List<PlanData>();
    }
    public class PlanData
    {
        public string keyword = "";
        public FilterMdoe filterMdoe = FilterMdoe.Default;
        public bool isAllMatch = false;
        public bool isActive = true;
    }
    
    public class PlanLayout : YKHorizontal
    {
        public int id;
        public PlanData data;

        public InputField keyWordInput;
        public UIDropdown drop;
        public UIButton allMatchButton;
        public UIButton activeButton;

        public GameObject allMatchObject;
        public GameObject spacerObject;
        public bool isShowIsAllMatch  = false;

        public void SetId(int i)
        {
            id = i;
        }
        public void UIUpdata(PlanData data)
        {
            keyWordInput.text = data.keyword;
            drop.value = (int)data.filterMdoe;
            if ((int)data.filterMdoe == 1)
            {
                isShowIsAllMatch = true;
            }
            else
            {
                isShowIsAllMatch = false;
            }
            RefreshState(isShowIsAllMatch);
            allMatchButton.SetCheck(data.isAllMatch);
            activeButton.SetCheck(data.isActive);
        }
        public void OnKeyWordInput(string value)
        {
            data.keyword = value;
        }
        public void OnDrop(int v)
        {
            data.filterMdoe = (AutoBuy.FilterMdoe)v;
            if (v == 1)
            {
                isShowIsAllMatch = true;
            }
            else
            {
                isShowIsAllMatch = false;
            }
            RefreshState(isShowIsAllMatch);
        }
        public void OnAllMatchButton(bool b)
        {
            if(data == null)
            {
                UDebug.Log("data为空");
            }
            data.isAllMatch = b;
        }
        public void OnActiveButton(bool b)
        {
            data.isActive = b;
        }
        public void RefreshState(bool b)
        {
            if (allMatchObject == null)
            {
                return;
            }
            spacerObject.SetActive(!b);
            allMatchObject.SetActive(b);
        }
        
    }
}
