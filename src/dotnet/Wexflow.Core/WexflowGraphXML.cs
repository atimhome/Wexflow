using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Wexflow.Core
{
    /// <summary>
    /// SetTask(If,Switch,While) to GraphXML
    /// </summary>
    public class WexflowGraphXML
    {
        /// <summary>
        /// XMLNamespace
        /// </summary>
        public XNamespace WexflowNamespace { get; private set; }

        /// <summary>
        /// All Tasks Infomation
        /// </summary>
        public List<XElement> TaskList { get; private set; }

        /// <summary>
        /// 建構式
        /// </summary>
        /// <param name="allTask">任務清單</param>
        public WexflowGraphXML(List<XElement> allTask)
        {
            this.WexflowNamespace = "urn:wexflow-schema";
            this.TaskList = allTask;
        }

        /// <summary>
        /// Get Setting Value from tasks
        /// </summary>
        /// <param name="elements">All tasks</param>
        /// <param name="name">setting name</param>
        /// <param name="valueName">Get Value name</param>
        /// <returns>Setting value</returns>
        public string GetSettingValue(List<XElement> elements, string name, string valueName = "value")
        {
            var xe = this._GetSettingXElement(elements, name);
            var output = (xe != null && xe.Attribute(valueName) != null) ? xe.Attribute(valueName).Value : null;

            return (output != null) ? output : string.Empty;
        }

        /// <summary>
        /// Get Graph XML
        /// </summary>
        /// <param name="elements">execute tasks</param>
        /// <param name="parentElement">parent Element</param>
        /// <param name="isRoot">Is root task</param>
        /// <param name="parent">parent ID</param>
        /// <returns></returns>
        public XElement GetGraphXElements(List<XElement> elements, XElement parentElement, bool isRoot, string parent = "-1")
        {
            XElement output = parentElement;            
            XElement nowElement = new XElement(WexflowNamespace + "Task");

            while (elements.Count > 0)
            {
                if (isRoot && this._GetXElement(elements, "parent", parent, out nowElement) == false)
                {
                    output = parentElement;
                    break;
                }

                if (isRoot == false)
                {
                    nowElement = elements[0];
                }

                output.Add(this._GetGraphXElement(nowElement, parent));
                elements.Remove(nowElement);
                parent = nowElement.Attribute("id").Value;
            }

            return output;
        }

        private bool _GetXElement(List<XElement> elements, string chAttr, string chValue, out XElement output)
        {
            output = elements
                .Where(n => n.Elements().ToList()
                    .Where(s => s.Attribute("name").Value == chAttr && s.Attribute("value").Value == chValue)
                    .Any()
                )
                .FirstOrDefault();

            return (output != null) ? true : false;
        }        

        private XElement _GetSettingXElement(List<XElement> elements, string name)
        {
            var output = elements.Where(n => n.Attribute("name").Value == name).FirstOrDefault();

            return output;
        }

        private List<XElement> _GetSettingXElements(List<XElement> elements, string name)
        {
            var output = elements.Where(n => n.Attribute("name").Value == name).ToList();
            // name="CaseTasks" value="1,2,3" case="A"
            // name="CaseTasks" value="1,2,3" case="B"

            return (output != null && output.Count > 0) ? output : new List<XElement>();
        }

        private XElement _GetGraphXElement(XElement nowEle, string parent)
        {
            
            var settingList = nowEle.Elements().ToList();

            switch (nowEle.Attribute("name").Value)
            {
                case "IfCheck":
                    // if
                    var xIf = new XElement(WexflowNamespace + "If"
                        , new XAttribute("id", nowEle.Attribute("id").Value)
                        , new XAttribute("parent", parent)
                        , new XAttribute("if", this.GetSettingValue(settingList, "checkID"))
                        );

                    // DO
                    var xDo = this._GetXml("Do", settingList, "trueTasks");
                    xIf.Add(xDo);

                    // Else
                    var xElse = this._GetXml("Else", settingList, "falseTasks");
                    xIf.Add(xElse);

                    return xIf;
                case "SwitchCheck":
                    // switch
                    var xSwitch = new XElement(WexflowNamespace + "Switch"
                        , new XAttribute("id", nowEle.Attribute("id").Value)
                        , new XAttribute("parent", parent)
                        , new XAttribute("switch", this.GetSettingValue(settingList, "checkID"))
                        );

                    // cases
                    var cases = this._GetXmls("Case", settingList, "caseTasks", "case");
                    xSwitch.Add(cases);

                    // default
                    var xDefault = this._GetXml("Default", settingList, "defaultTasks");
                    xSwitch.Add(xDefault);

                    return xSwitch;
                case "WhileCheck":
                    // while
                    var xWhile = new XElement(WexflowNamespace + "While"
                        , new XAttribute("id", nowEle.Attribute("id").Value)
                        , new XAttribute("parent", parent)
                        , new XAttribute("while", this.GetSettingValue(settingList, "checkID"))
                        );

                    // children tasks
                    xWhile = this._GetChilerenXml(settingList, "whileTasks", xWhile);

                    return xWhile;
                default:
                    var xTask = new XElement(WexflowNamespace + "Task", new XAttribute("id", nowEle.Attribute("id").Value));
                    var xTaskChild = new XElement(WexflowNamespace + "Parent", new XAttribute("id", parent));

                    xTask.Add(xTaskChild);

                    return xTask;
            }
        }

        private List<XElement> _GetXmls(string tagName, List<XElement> settingList, string settingName, string tagAttribute = "")
        {

            List<XElement> xElmList = new List<XElement>();

            var settingXMLs = this._GetSettingXElements(settingList, settingName);

            foreach (var item in settingXMLs)
            {
                var xElm = this._GetXml(tagName, new List<XElement>() { item }, settingName, tagAttribute);
                xElmList.Add(xElm);
            }

            return xElmList;
        }

        private XElement _GetXml(string tagName, List<XElement> settingList, string settingName, string tagAttribute = "")
        {
            
            var xElm = new XElement(WexflowNamespace + tagName);
            if (string.IsNullOrWhiteSpace(tagAttribute) == false)
            {
                var attributeValue = this.GetSettingValue(settingList, settingName, tagAttribute);
                xElm.Add(new XAttribute("value", attributeValue));
            }

            xElm = this._GetChilerenXml(settingList, settingName, xElm);

            return xElm;
        }

        private XElement _GetChilerenXml(List<XElement> settingList, string settingName, XElement parentElement)
        {
            XElement output = parentElement;

            var taskArray = this.GetSettingValue(settingList, settingName).Split(',');

            //childElement
            List<XElement> elmList = new List<XElement>();
            foreach (var s in taskArray)
            {
                var chElement = this._GetElementByID(s);
                elmList.AddRange(chElement);
            }

            output = this.GetGraphXElements(elmList, output, false);

            return output;
        }

        private List<XElement> _GetElementByID(string id)
        {
            var output = new List<XElement>();

            var element = this.TaskList.Where(
                   l => l.Attribute("id").Value == id
               ).FirstOrDefault();

            if (element == null)
                return output;

            output.Add(element);
            output.AddRange(this._GetElementByParentID(id));

            return output;
        }

        private List<XElement> _GetElementByParentID(string parentId)
        {
            var output = new List<XElement>();

            var element = this.TaskList.Where(
                   e => this.GetSettingValue(e.Elements().ToList(), "parent") == parentId
               ).FirstOrDefault();

            if (element == null)
                return output;
            
            var chElement = this._GetElementByParentID(element.Attribute("id").Value);

            output.Add(element);
            output.AddRange(chElement);

            return output;
        }
    }
}
