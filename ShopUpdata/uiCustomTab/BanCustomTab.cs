using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YKF;
using UDebug = UnityEngine.Debug;
namespace AutoBuy
{
    public class BanCustomTab : YKLayout<object>
    {
        public List<string> filterList = new List<string> { "Default", "Name", "Detail", "Tags", "Element", "StockNum" };
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
            Header(mainLayout);
            StaticBuild();
        }
        public YKLayout Header(YKLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            ho.Header("IsActive");
            ho.Spacer(1, 40);
            ho.Header("keyWord");
            ho.Spacer(1, 40);
            ho.Header("FilterMdoe");
            ho.Spacer(1, 40);
            ho.Header("IsAllMatch");
            YKVertical verticalButtonLayout = ho.Vertical();
            UIButton deleteAllButton = ho.Button("DeleteAll", () => { RemoveAll(); });
            SetSize(deleteAllButton, 50, 90);
            //ho.Spacer(1, 10);
            return ho;
        }
        public YKLayout IsActiveItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            father.activeButton = ho.Toggle((father.id + 1).ToString(), isOn: true, onClick: father.OnActiveButton);
            float offset = 20;
            Transform textTransform = father.activeButton.mainText.transform;
            Transform imageTransform = father.activeButton.transform.Find("Image");
            textTransform.position -= Vector3.right * offset * 2;
            imageTransform.position += Vector3.right * offset * 1.2f;
            SetSize(father.activeButton, 36, 60);
            return ho;
        }
        public YKLayout keyWordItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            //var text = ho.Text("keyWord", color: FontColor.DontChange);
            //SetSize(text, w: textWidth);
            string s = "";
            //father.keyWordInput = UIInputSet(s, layOut: ho, onInput: father.OnKeyWordInput);
            father.keyWordInput = ConfigLayer.InputItem(father, "hello", father.OnKeyWordInput).field;
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
            father.allMatchButton = ho.Toggle("", isOn: false, onClick: father.OnAllMatchButton);
            SetSize(father.allMatchButton, 36, 60);
            return ho;

        }
        public YKLayout RemoveItem(PlanLayout father)
        {
            YKHorizontal ho = father.Horizontal();
            father.deleteButton = ho.Button("Delete", () =>
            {
                Remove(father);
            });
            return ho;
        }
        public YKLayout PlanItem(int id, YKLayout father = null)
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

            //planeList.Add(ho);
            return ho;
        }
        public void SetSize(Component com, float h = 36, float w = 100)
        {
            com.Rect().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            com.GetOrCreate<LayoutElement>().preferredWidth = w;
        }
        //public InputField UIInputSet(string text, Action<string> onInput = null, YKLayout layOut = null)
        //{

        //    Widget widget = Util.Instantiate<Widget>("UI/Widget/WidgetSearch", this)
        //      ?? Util.Instantiate<Widget>("UI/Widget/WidgetSearch/WidgetSearch", this);
        //    widget.gameObject.SetActive(false);
        //    Transform component = widget.transform.Find("Search Box");
        //    Transform d1 = widget.transform.Find("Search Box/ButtonGeneral");
        //    Transform d2 = widget.transform.Find("Search Box/ButtonGeneral (1)");
        //    component.SetParent(layOut.transform);
        //    widget.transform.DestroyObject();

        //    d1.DestroyObject();
        //    d2.DestroyObject();

        //    InputField input = component.Find("InputField").GetComponent<InputField>();

        //    if (onInput != null)
        //    {
        //        input.onValueChanged.AddListener(value => onInput?.Invoke(value));
        //    }
        //    input.text = text;
        //    input.gameObject.GetComponent<CanvasRenderer>().SetColor(new Color(0.8f, 0.8f, 0.8f, 0.2f));
        //    input.Find("Text").gameObject.GetComponent<Text>().color = new Color(0.28f, 0.22f, 0.13f, 1f);
        //    component.Rect().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40f);
        //    component.GetOrCreate<LayoutElement>().preferredWidth = 125f;
        //    return input;
        //}
        public PlanLayout CreatePlanLayout(Transform father)
        {
            PlanLayout layout = YK.Create<PlanLayout>(father);
            layout.OnLayout();
            return layout;
        }
        public void AddPlan()
        {
            PlanData newData = new PlanData();
            AutoBuy.configeData.banPlanList.Add(newData);
            RefreshList();
            /*AutoBuy.ReBuildUI();*/
            AutoBuy.layer.banSettingTab.scrollRect.normalizedPosition = Vector3.zero;

        }
        public void Remove(PlanLayout planLayout)
        {
            AutoBuy.configeData.banPlanList.Remove(planLayout.data);
            /*AutoBuy.ReBuildUI();*/
            RefreshList();
        }
        public void RemoveAll()
        {
            Dialog.YesNo("Are you sure you want to delete all?", () => {
                AutoBuy.configeData.banPlanList.Clear();
                /*AutoBuy.ReBuildUI();*/
                RefreshList();
            });
        }
        public void StaticBuild()
        {
            for (int i = 0; i < planCount; i++)
            {
                PlanLayout planLayout = PlanItem(i, this) as PlanLayout;
                //PlanLayout planLayout = PoolHelp.SpawnPlanLayout(this.transform,i);
                planLayout.ReBindUiEvent(this);
                planeList.Add(planLayout);
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
            for (int i = 0; i < AutoBuy.configeData.banPlanList.Count; i++)
            {
                PlanLayout item = PlanItem(i, this) as PlanLayout;
                item.data = AutoBuy.configeData.banPlanList[i];
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
            for (int i = 0; i < AutoBuy.configeData.banPlanList.Count; i++)
            {
                if (i > planCount - 1) { UDebug.Log("大于了列表设置上限"); break; }
                planeList[i].data = AutoBuy.configeData.banPlanList[i];
                planeList[i].UIUpdata(AutoBuy.configeData.banPlanList[i]);
                planeList[i].gameObject.SetActive(true);
            }
            RefreshAddButton();
        }
        public void RefreshAddButton()
        {
            if (AutoBuy.configeData.banPlanList.Count >= planCount)
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
}
