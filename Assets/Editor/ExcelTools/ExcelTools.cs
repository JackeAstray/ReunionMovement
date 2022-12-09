using Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace GameLogic.Editor
{
    public class ExcelTools
    {
        /// <summary>
        /// ��ǰ�༭������ʵ��
        /// </summary>
        private static ExcelTools instance;

        static readonly string toDir = "./Config/ExcelTools";                                       // Դ�ļ�·��
        static readonly string scriptOutPutPath = "Assets/HotUpdate/Scripts/DataBase/AutoScript/";  // �ű����·��
        static readonly string dataOutPutPath = "Assets/HotUpdate/Resources/Datas/";                // ���ݱ����·��

        static int tableRows_Max = 3;                                           // �������
        static int tableRows_1 = 0;                                             // ��һ����������
        static int tableRows_2 = 1;                                             // �ڶ�����������
        static int tableRows_3 = 2;                                             // ������Ӣ������

        #region ��� -> �ű�
        [MenuItem("������/�����/��� -> �ű�", false, 1)]
        public static void ExcelToScripts()
        {
            List<string> xlsxFiles = GetAllConfigFiles();

            foreach (var path in xlsxFiles)
            {
                ExcelToScripts(path);
            }

            Debug.Log("���תΪ�ű���ɣ�");
        }
        /// <summary>
        /// Excel���ű�
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static bool ExcelToScripts(string path)
        {

            //����Excel������
            ExcelUtility excel = new ExcelUtility(path);

            if (excel.ResultSet == null)
            {
                string msg = string.Format("�޷���ȡ��{0}�����ƺ��ⲻ��һ��xlsx�ļ�!", path);
                //EditorUtility.DisplayDialog("ExcelToScriptableObject", msg, "OK");
                return false;
            }

            List<SheetData> sheets = new List<SheetData>();
            //���������
            foreach (DataTable table in excel.ResultSet.Tables)
            {
                string tableName = table.TableName.Trim();
                //�жϱ�����ǰ���Ƿ���#  �������
                if (tableName.StartsWith("#"))
                {
                    continue;
                }

                SheetData sheet = new SheetData();
                sheet.table = table;

                if (table.Rows.Count < tableRows_Max)
                {
                    EditorUtility.ClearProgressBar();
                    string msg = string.Format("�޷�������{0}����Excel�ļ�Ӧ���ٰ������У���һ�У�Ӣ�����ƣ��ڶ��У��������ƣ������У��������ͣ�!", path);
                    EditorUtility.DisplayDialog("ExcelToScriptableObject", msg, "OK");
                    return false;
                }
                //��������
                sheet.itemClassName = tableName;

                if (!Tools.CheckClassName(sheet.itemClassName))
                {
                    EditorUtility.ClearProgressBar();
                    string msg = string.Format("���������ơ�{0}����Ч����Ϊ�ù����������ӦΪ����!", sheet.itemClassName);
                    EditorUtility.DisplayDialog("ExcelToScriptableObject", msg, "OK");
                    return false;
                }
                //�ֶ�����
                object[] fieldNames;
                fieldNames = table.Rows[tableRows_3].ItemArray;
                //�ֶ�ע��
                object[] fieldNotes;
                fieldNotes = table.Rows[tableRows_1].ItemArray;
                //�ֶ�����
                object[] fieldTypes;
                fieldTypes = table.Rows[tableRows_2].ItemArray;

                for (int i = 0, imax = fieldNames.Length; i < imax; i++)
                {
                    string fieldNameStr = fieldNames[i].ToString().Trim();
                    string fieldNoteStr = fieldNotes[i].ToString().Trim();
                    string fieldTypeStr = fieldTypes[i].ToString().Trim();
                    //����ֶ���
                    if (string.IsNullOrEmpty(fieldNameStr))
                    {
                        break;
                    }
                    if (!Tools.CheckFieldName(fieldNameStr))
                    {
                        EditorUtility.ClearProgressBar();
                        string msg = string.Format("�޷�������{0}������Ϊ�ֶ�����{1}����Ч!", path, fieldNameStr);
                        EditorUtility.DisplayDialog("ExcelToScriptableObject", msg, "OK");
                        return false;
                    }

                    //��������
                    FieldTypes fieldType = GetFieldType(fieldTypeStr);

                    FieldData field = new FieldData();
                    field.fieldName = fieldNameStr;
                    field.fieldNotes = fieldNoteStr;
                    field.fieldIndex = i;
                    field.fieldType = fieldType;
                    field.fieldTypeName = fieldTypeStr;

                    if (fieldType == FieldTypes.Unknown)
                    {
                        fieldType = FieldTypes.UnknownList;
                        if (fieldTypeStr.StartsWith("[") && fieldTypeStr.EndsWith("]"))
                        {
                            fieldTypeStr = fieldTypeStr.Substring(1, fieldTypeStr.Length - 2).Trim();
                        }
                        else if (fieldTypeStr.EndsWith("[]"))
                        {
                            fieldTypeStr = fieldTypeStr.Substring(0, fieldTypeStr.Length - 2).Trim();
                        }
                        else
                        {
                            fieldType = FieldTypes.Unknown;
                        }

                        field.fieldType = field.fieldType == FieldTypes.UnknownList ? FieldTypes.CustomTypeList : FieldTypes.CustomType;
                    }

                    sheet.fields.Add(field);
                }

                sheets.Add(sheet);
            }

            for (int i = 0; i < sheets.Count; i++)
            {
                GenerateScript(sheets[i]);
            }

            return true;
        }

        /// <summary>
        /// ���ɽű�
        /// </summary>
        /// <param name="sheet"></param>
        static async void GenerateScript(SheetData sheet)
        {
            string ScriptTemplate = @"//�˽ű�Ϊ�Զ����� <ExcelToScript>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameLogic
{
    
    [Serializable]
    public class {_0_}
    {
        {_1_}

        public override string ToString()
        {
            return string.Format(
                {_2_},
                {_3_}
            );
        }
    }
}
";
            var dataName = sheet.itemClassName;
            var str = GenerateDataScript(ScriptTemplate, dataName, sheet.fields);
            await Tools.SaveFile(scriptOutPutPath + dataName + ".cs", str);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// �������ݽṹ�ű�
        /// </summary>
        /// <param name="template"></param>
        /// <param name="scriptName"></param>
        /// <param name="fieldDatas"></param>
        /// <returns></returns>
        static string GenerateDataScript(string template, string scriptName, List<FieldData> fieldDatas)
        {
            StringBuilder privateType = new StringBuilder();
            privateType.AppendLine();

            string toString_1 = "";
            string toString_2 = "";

            string additional = "{{ get; set; }}";

            for (int i = 0; i < fieldDatas.Count; i++)
            {
                var typeName = GetFieldTypeString(fieldDatas[i].fieldType, fieldDatas[i].fieldTypeName);

                string attribute = string.Format("        public {0} {1} {2}    //{3}", typeName, fieldDatas[i].fieldName, additional, fieldDatas[i].fieldNotes);
                privateType.AppendFormat(attribute);
                privateType.AppendLine();

                int value = i + 1;
                toString_1 += fieldDatas[i].fieldName + "={" + value + "}";
                if (i < fieldDatas.Count - 1)
                    toString_1 += ",";

                toString_2 += "this." + fieldDatas[i].fieldName;
                if (i < fieldDatas.Count - 1)
                    toString_2 += ",\r\n                ";

            }

            string str = template;
            str = str.Replace("{_0_}", scriptName);
            str = str.Replace("{_1_}", privateType.ToString());
            str = str.Replace("{_2_}", "\"[" + toString_1 + "]\"");
            str = str.Replace("{_3_}", toString_2);
            return str;
        }
        #endregion

        #region ��� -> Json
        [MenuItem("������/�����/��� -> JSON", false, 2)]
        public static void ExcelToJson()
        {
            List<string> xlsxFiles = GetAllConfigFiles();

            foreach (var path in xlsxFiles)
            {
                ExcelToJsons(path);
            }

            Debug.Log("���תΪJson��ɣ�");
        }

        /// <summary>
        /// Excel ת Json
        /// </summary>
        /// <param name="path"></param>
        public static void ExcelToJsons(string path)
        {
            //dataOutPutPath
            //�ȴ��������
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("����", "�ȴ����������", "OK");
                return;
            }

            //�鿴·���Ƿ����
            if (Directory.Exists(dataOutPutPath) == false)
            {
                Directory.CreateDirectory(dataOutPutPath);
            }

            //����Excel������
            ExcelUtility excel = new ExcelUtility(path);

            if (excel.ResultSet == null)
            {
                string msg = string.Format("�ļ���{0}�����Ǳ��", path);
                return;
            }

            //��ȡExcel�ļ��ľ���·��
            List<string> strArray = path.Split('\\').ToList();
            string output = dataOutPutPath + strArray[strArray.Count - 1];
            output = output.Replace(".xlsx", ".json");
            excel.ConvertToJson(output);

            //ˢ�±�����Դ
            AssetDatabase.Refresh();
        }
        #endregion

        //----------------------����----------------------

        /// <summary>
        /// ��ȡ���е�xlsx�ļ�·��
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllConfigFiles(string filetype = "*.xlsx")
        {
            List<string> tableList = new List<string>();
            //�ȴ��������
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("����", "�ȴ����������", "OK");
                return null;
            }
            //�鿴·���Ƿ����
            if (Directory.Exists(toDir) == false)
            {
                Directory.CreateDirectory(toDir);
                return null;
            }
            //�����ļ�Ŀ¼
            foreach (var path in Directory.GetFiles(toDir, "*", SearchOption.AllDirectories))
            {
                var suffix = Path.GetExtension(path);
                if (suffix != ".xlsx" && suffix != ".xls")
                {
                    string msg = string.Format("�ļ���{0}�����Ǳ��", path);
                    //EditorUtility.DisplayDialog("ExcelToScriptableObject", msg, "OK");
                    continue;
                }
                tableList.Add(path);
            }

            if(tableList.Count <= 0)
            {
                Debug.LogWarning("û���ҵ����");
            }

            return tableList;
        }

        /// <summary>
        /// ��ȡ�ֶ�����
        /// </summary>
        /// <param name="typename"></param>
        /// <returns></returns>
        static FieldTypes GetFieldType(string typeName)
        {
            FieldTypes type = FieldTypes.Unknown;
            if (!string.IsNullOrEmpty(typeName))
            {
                switch (typeName.Trim().ToLower())
                {
                    case "bool":
                        type = FieldTypes.Bool;
                        break;
                    case "int":
                    case "int32":
                        type = FieldTypes.Int;
                        break;
                    case "ints":
                    case "int[]":
                    case "[int]":
                    case "int32s":
                    case "int32[]":
                    case "[int32]":
                        type = FieldTypes.Ints;
                        break;
                    case "float":
                        type = FieldTypes.Float;
                        break;
                    case "floats":
                    case "float[]":
                    case "[float]":
                        type = FieldTypes.Floats;
                        break;
                    case "long":
                    case "int64":
                        type = FieldTypes.Long;
                        break;
                    case "longs":
                    case "long[]":
                    case "[long]":
                    case "int64s":
                    case "int64[]":
                    case "[int64]":
                        type = FieldTypes.Longs;
                        break;
                    case "vector2":
                        type = FieldTypes.Vector2;
                        break;
                    case "vector3":
                        type = FieldTypes.Vector3;
                        break;
                    case "vector4":
                        type = FieldTypes.Vector4;
                        break;
                    case "rect":
                    case "rectangle":
                        type = FieldTypes.Rect;
                        break;
                    case "color":
                    case "colour":
                        type = FieldTypes.Color;
                        break;
                    case "string":
                        type = FieldTypes.String;
                        break;
                    case "strings":
                    case "string[]":
                    case "[string]":
                        type = FieldTypes.Strings;
                        break;
                }
            }
            return type;
        }

        /// <summary>
        /// ��ȡ�ֶ�����
        /// </summary>
        /// <param name="fieldTypes"></param>
        /// <returns></returns>
        static string GetFieldTypeString(FieldTypes fieldTypes, string fieldTypeName)
        {
            string result = string.Empty;
            switch (fieldTypes)
            {
                case FieldTypes.Bool:
                    result = "bool";
                    break;
                case FieldTypes.Int:
                    result = "int";
                    break;
                case FieldTypes.Ints:
                    result = "List<int>";
                    break;
                case FieldTypes.Float:
                    result = "float";
                    break;
                case FieldTypes.Floats:
                    result = "List<float>";
                    break;
                case FieldTypes.Long:
                    result = "long";
                    break;
                case FieldTypes.Longs:
                    result = "List<long>";
                    break;
                case FieldTypes.Vector2:
                    result = "Vector2";
                    break;
                case FieldTypes.Vector3:
                    result = "Vector3";
                    break;
                case FieldTypes.Vector4:
                    result = "Vector4";
                    break;
                case FieldTypes.Rect:
                    result = "Rect";
                    break;
                case FieldTypes.Color:
                    result = "Color";
                    break;
                case FieldTypes.String:
                    result = "string";
                    break;
                case FieldTypes.Strings:
                    result = "List<string>";
                    break;
                case FieldTypes.CustomType:
                    result = "fieldTypeName";
                    break;
                case FieldTypes.CustomTypeList:
                    result = "List<fieldTypeName>";
                    break;
            }

            return result;
        }
    }

    /// <summary>
    /// ���ű�����
    /// </summary>
    public class SheetData
    {
        public DataTable table;
        public string itemClassName;
        public bool keyToMultiValues;
        public bool internalData;
        public List<FieldData> fields = new List<FieldData>();
    }

    /// <summary>
    /// �ֶ�����
    /// </summary>
    public class FieldData
    {
        public string fieldName;        //�ֶ�����
        public string fieldNotes;       //�ֶ�ע��
        public int fieldIndex;          //�ֶ�����
        public FieldTypes fieldType;    //�ֶ�����
        public string fieldTypeName;    //�ֶ���������
    }
}