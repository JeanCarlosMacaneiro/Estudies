using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

public class XmlSerializable
{
    #region Private Variables
    private List<string> fatherReference = new List<String>();
    private List<XElement> xmlComponentsSons = new List<XElement>();
    private XElement objectXml;

    private int CurrentFatherIndex = 0;
    private string xmlString = "";

    #endregion

    #region Constants
    private const int STRING_VALUE_DOUBLE_QUOTES_DEC = 34;
    private const string VALUE_ZERO = "0";
    private const string CHARACTER_GREATER_THAN_ESCAPE = "&gt;";
    private const string CHARACTER_LESS_THAN_ESCAPE = "&lt;";
    private const string CHARACTER_AMPERSAND_ESCAPE = "&amp;";
    private const string CHARACTER_DOUBLE_QUOTES_ESCAPE = "&quot;";
    private const string CHARACTER_QUOTE_ESCAPE = "&apos;";

    private const string CHARACTER_GREATER_THAN = ">";
    private const string CHARACTER_LESS_THAN = "<";
    private const string CHARACTER_AMPERSAND = "&";
    private const string STRING_VALUE_DOUBLE_QUOTES = "\"";
    private const string CHARACTER_QUOTE = "'";
    private const string DECLARATION_DEFAULT_XML = " ";//"<?xml version=" + STRING_VALUE_DOUBLE_QUOTES + "1.0" + STRING_VALUE_DOUBLE_QUOTES + " encoding=" + STRING_VALUE_DOUBLE_QUOTES + "utf-8" + STRING_VALUE_DOUBLE_QUOTES + " ?>";


    #endregion

    public string createXmlString(Object value/* Object type abstact*/, string nameXml)
    {

        /*
         * declaration default TAG
         */
        initializeObjecXml(nameXml);

        /*
         * create all tags Xml in respected hierarchy Object
         */
        serializeXML(value);

        /*
         * Mount string return
         */
        MountScringXml();

        return xmlString;
    }

    #region Create XML
    private void serializeXML(Object objectFather /* Object type abstact*/)
    {
        foreach (var property in objectFather.GetType().GetProperties())
        {
            var propertyNameOld = string.Empty;
            string propertyValue;
            Type propertyType; ;
            try
            {
                propertyType = property.GetValue(objectFather, null).GetType();

                if (propertyType == typeof(int) || propertyType == typeof(string))
                {
                    propertyValue = property.GetValue(objectFather, null).ToString();

                    if (validateValues(propertyValue, property))
                    {
                        addComponetXml(property.Name, propertyValue);
                    }
                }
                else if (propertyType.IsGenericType)
                {
                    object objectGenericCollection = property.GetValue(objectFather, null);
                    object objectlistCreated = objectGenericCollection.GetType().GetProperty("Created").GetValue(objectGenericCollection, null);
                    object objectlistUpdated = objectGenericCollection.GetType().GetProperty("Updated").GetValue(objectGenericCollection, null);
                    IList iList = null;

                    if (objectlistCreated != null)
                    {
                        iList = (IList)objectlistCreated;

                        if (iList != null && iList.Count > 0)
                        {
                            getElementsList(iList, property.Name);
                        }
                    }

                    if (objectlistUpdated != null)
                    {
                        iList = (IList)objectlistUpdated;

                        if (iList != null && iList.Count > 0)
                        {
                            getElementsList(iList, property.Name);
                        }
                    }
                }
                else if (propertyType.BaseType == typeof(objectChild)) //Is object children type for recursive functions    
                {
                    //create the father
                    addComponetXml(property.Name, string.Empty, true);

                    object objectValueChild = property.GetValue(objectValueFather, null);
                    serializeXML((Object/* Object type abstact*/)objectBusinessValueChild);

                    //back to father old
                    backCurrentFatherIdex();
                }
            }

            catch (Exception ex) { }
        }
    }

    #endregion

    #region logic mount nodes GenericList
    private void getElementsList(IList list, string propertyName)
    {
        foreach (var objectList in list)
        {
            addComponetXml(propertyName, string.Empty, true);

            foreach (var propertyList in objectList.GetType().GetProperties())
            {
                string propertyListValue = propertyList.GetValue(objectList, null).ToString();

                addComponetXml(propertyList.Name, propertyListValue);
            }
        }
        backCurrentFatherIdex();
    }
    #endregion

    #region Mount XML nodes
    private void addComponetXml(String name, string value, bool isFather = false)
    {
        if (isFather)
        {
            //add list childrens in the father old 
            if (xmlComponentsSons != null && xmlComponentsSons.Count > 0)
            {
                objectXml.Add(new XElement(fatherReference[CurrentFatherIndex], xmlComponentsSons));
                //clear list Sons
                xmlComponentsSons.Clear();
            }
            //crete a father
            fatherReference.Add(name);
            CurrentFatherIndex = fatherReference.Count() - 1;
        }
        else
        {
            //crete new node in list
            xmlComponentsSons.Add(new XElement(name, value));
        }
    }

    private void backCurrentFatherIdex()
    {
        if (CurrentFatherIndex > 0)
        {
            CurrentFatherIndex -= 1;
        }
    }


    private void MountScringXml()
    {
        xmlString = objectXml.ToString();
    }

    private void initializeObjecXml(string nameXml)
    {
        /*
         * insitantiate objectXml
         */
        XDocument doc = new XDocument();
        XNamespace attributHeader = @"Xmlns:" + string.Format(DECLARATION_DEFAULT_E_SOCIAL_ATTRIBUTE, nameXml);

        string eSocialHeader = "nameXml";

        objectXml = new XElement(eSocialHeader, " ");

        /*
         * instantiate Father
         */
        fatherReference.Add(eSocialHeader);
    }
    #endregion

    #region verify values
    private bool validateValues(string value, PropertyInfo propertyInfo)
    {
        bool result = true;

        result = checkValuesNull(value);
        result &= checkAttributesProperty(propertyInfo, value);
        result &= valueConsistencyCheck(value);

        return result;
    }


    private bool valueConsistencyCheck(string value)
    {
        bool result = true;

        //verify values with spaces 
        value = value.Trim();

        //Change Values for ESCAPE XML
        foreach (var character in value.ToCharArray())
        {
            switch (character.ToString())
            {
                case CHARACTER_GREATER_THAN:
                    value.Replace(character.ToString(), CHARACTER_GREATER_THAN_ESCAPE);
                    break;
                case CHARACTER_LESS_THAN:
                    value.Replace(character.ToString(), CHARACTER_LESS_THAN_ESCAPE);
                    break;
                case CHARACTER_AMPERSAND:
                    value.Replace(character.ToString(), CHARACTER_AMPERSAND_ESCAPE);
                    break;
                case STRING_VALUE_DOUBLE_QUOTES:
                    value.Replace(character.ToString(), CHARACTER_DOUBLE_QUOTES_ESCAPE);
                    break;
                case CHARACTER_QUOTE:
                    value.Replace(character.ToString(), CHARACTER_QUOTE_ESCAPE);
                    break;
            }
        }
        return result;
    }

    private bool checkAttributesProperty(PropertyInfo propertyInfo, string value)
    {
        //Verify attribute [XmlIgnore]
        bool result = true;
        var pi = propertyInfo;
        var hasIsIdentity = Attribute.IsDefined(pi, typeof(XmlIgnoreAttribute));

        if (hasIsIdentity)
        {
            result = false;
        }
        //Verify attribute [ValidZeroValueXmlSerializable]
        else
        {
            hasIsIdentity = Attribute.IsDefined(pi, typeof(ValidZeroValueXmlSerializable));
            if (!hasIsIdentity)
            {
                if (value.Equals(VALUE_ZERO))
                {
                    result = false;
                }
            }
        }

        return result;
    }

    private bool checkValuesNull(string value)
    {
        bool result = true;

        if (string.IsNullOrEmpty(value) || value.Equals(DateTime.MinValue))
        {
            result = false;
        }
        return result;

    }
    #endregion

}

