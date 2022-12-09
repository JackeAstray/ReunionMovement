using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEngine;
using GameLogic;
using System;
using System.Reflection;
using System.Text;
using Excel;
using GameLogic.Json;

namespace GameLogic.Editor
{
    public class ExcelUtility
    {
        /// <summary>
        /// ������ݼ���
        /// </summary>
        private DataSet mResultSet;

        /// <summary>
        /// ���ڻ�ȡ�������
        /// </summary>
        public DataSet ResultSet { get { return mResultSet; } }

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="excelFile">Excel file.</param>
        public ExcelUtility(string path)
        {
            //ͨ���ļ���ȡ����
            string className = Path.GetFileNameWithoutExtension(path);

            //��������Ƿ�������
            if (!Tools.CheckClassName(className))
            {
                string msg = string.Format("Excel�ļ���{0}����Ч����Ϊxlsx�ļ�������ӦΪ������", path);
                Debug.Log(msg);
                return;
            }

            //����һ���ļ�
            int indexOfDot = path.LastIndexOf('.');
            string tempExcel = string.Concat(path.Substring(0, indexOfDot), "_temp_", path.Substring(indexOfDot, path.Length - indexOfDot));
            File.Copy(path, tempExcel);

            //��ȡ�������ļ�
            Stream stream = null;
            try
            {
                stream = File.OpenRead(tempExcel);
            }
            catch
            {
                File.Delete(tempExcel);
                string msg = string.Format("���ڹ����ͻ���޷��򿪡�{0}����Ҳ����Ӧ���ȹر�ExcelӦ�ó���", path);
                Debug.Log(msg);
                return;
            }

            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            mResultSet = reader.AsDataSet();

            reader.Dispose();
            stream.Close();
            File.Delete(tempExcel);
        }

        /// <summary>
        /// ת��Ϊʵ�����б�
        /// </summary>
        public List<T> ConvertToList<T>()
        {
            //�ж�Excel�ļ����Ƿ�������ݱ�
            if (mResultSet.Tables.Count < 1) return null;
            //Ĭ�϶�ȡ��һ�����ݱ�
            DataTable mSheet = mResultSet.Tables[0];

            //�ж����ݱ����Ƿ��������
            if (mSheet.Rows.Count < 1) return null;

            //��ȡ���ݱ�����������
            int rowCount = mSheet.Rows.Count;
            int colCount = mSheet.Columns.Count;

            //׼��һ���б��Ա���ȫ������
            List<T> list = new List<T>();

            //��ȡ����
            for (int i = 1; i < rowCount; i++)
            {
                //����ʵ��
                Type t = typeof(T);
                ConstructorInfo ct = t.GetConstructor(System.Type.EmptyTypes);
                T target = (T)ct.Invoke(null);
                for (int j = 0; j < colCount; j++)
                {
                    //��ȡ��1��������Ϊ��ͷ�ֶ�
                    string field = mSheet.Rows[1][j].ToString();
                    object value = mSheet.Rows[i][j];
                    //��������ֵ
                    SetTargetProperty(target, field, value);
                }

                //������б�
                list.Add(target);
            }

            return list;
        }

        /// <summary>
        /// ת��ΪJson
        /// </summary>
        /// <param name="JsonPath">Json�ļ�·��</param>
        /// <param name="Header">��ͷ����</param>
        public async void ConvertToJson(string JsonPath/*, Encoding encoding*/)
        {
            var json = GetJson();
            //д���ļ�
            await Tools.SaveFile(JsonPath, json);
        }

        /// <summary>
        /// �󲹵Ľӿڣ�����ɵĵ���
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="IdX"></param>
        /// <param name="IdY"></param>
        /// <returns></returns>
        public string GetJson()
        {
            int x = -1;
            int y = -1;
            //var list = new List<object>();
            var json = GetJson(ref x, ref y/*, ref list*/);
            return json;
        }

        /// <summary>
        /// ��ȡjson
        /// </summary>
        /// <returns></returns>
        public string GetJson(ref int IdX, ref int IdY/*, ref List<object> keepFieldList*/)
        {
            IdX = -1;
            IdY = -1;

            //�ж�Excel�ļ����Ƿ�������ݱ�
            if (mResultSet == null || mResultSet.Tables == null)
            {
                return "";
            }

            //�ж�Excel�ļ����Ƿ�������ݱ�
            if (mResultSet.Tables.Count < 1)
            {
                return "";
            }

            //Ĭ�϶�ȡ��һ�����ݱ�
            DataTable mSheet = mResultSet.Tables[0];

            //�ж����ݱ����Ƿ��������
            if (mSheet.Rows.Count < 1)
            {
                return "";
            }

            //׼��һ���б�洢�����������
            List<Dictionary<string, object>> table = new List<Dictionary<string, object>>();
            /************Keep * Mode ������*������ ********************/
            /*
             *    Id   |   xxx |
             *    1    |   xxx |
             *    2    |   xxx |
             */

            ////ÿ�е�����
            //List<object> rowDatas = new List<object>();
            //�ֶ�����
            List<object> fieldNameRowDatas = new List<object>();
            //�ֶ�����
            List<object> fieldTypeRowDatas = new List<object>();
            //��һ��Ϊ��ע��
            //Ѱ�ҵ�id�ֶ�����������ȫΪ����
            int skipRowCount = -1;
            int skipColCount = -1;

            //����skip ��ֹ������ ��ע��ֱ������id
            int skipLine = 1;

            for (int i = skipLine; i < 10 && skipColCount == -1; i++)
            {
                var rows = this.GetRowDatas(i);
                //����rows
                for (int j = 0; j < rows.Count; j++)
                {
                    if (rows[j].Equals("Id"))
                    {
                        skipRowCount = i;
                        skipColCount = j;
                        fieldNameRowDatas = rows;
                        //��ȡ�ֶ�����
                        var rowtype = this.GetRowDatas(i - 1);
                        fieldTypeRowDatas = rowtype;
                        //
                        break;
                    }
                }
            }


            if (skipRowCount == -1)
            {
                Debug.LogError("������ݿ����д�,û����Id�ֶ�,����");
                return "{}";
            }

            int count = mSheet.Rows.Count;

            IdX = skipColCount;
            IdY = skipRowCount;

            //��ȡ����
            for (int i = skipRowCount + 1; i < mSheet.Rows.Count; i++)
            {
                //׼��һ���ֵ�洢ÿһ�е�����
                Dictionary<string, object> row = new Dictionary<string, object>();
                //
                for (int j = skipColCount; j < mSheet.Columns.Count; j++)
                {
                    string field = fieldNameRowDatas[j].ToString();
                    //�������ֶ�
                    if (string.IsNullOrEmpty(field))
                    {
                        continue;
                    }

                    //Key-Value��Ӧ
                    var rowdata = mSheet.Rows[i][j];
                    //����null�ж�
                    if (rowdata == null)
                    {
                        Debug.LogErrorFormat("�������Ϊ�գ�[{0},{1}]", i, j);
                        continue;
                    }

                    var fieldType = fieldTypeRowDatas[j].ToString().ToLower();
                    if (rowdata is DBNull) //�������жϣ���Ĭ��ֵ
                    {
                        if (fieldType == "int" || fieldType == "float" || fieldType == "double")
                        {
                            row[field] = 0;
                        }
                        else if (fieldType == "string")
                        {
                            row[field] = "";
                        }
                        else if (fieldType == "bool")
                        {
                            row[field] = false;
                        }
                        else if (fieldType.Contains("[]")) //������
                        {
                            row[field] = "[]";
                        }
                    }
                    else
                    {
                        //string���飬�Ե���Ԫ�ؼ���""
                        if (fieldType == "string[]")
                        {
                            var value = rowdata.ToString();
                            if (value != "[]" && !value.Contains("\"")) //���ǿ�����,��û��""
                            {
                                if (value.StartsWith("\"["))
                                {
                                    value = value.Replace("\"[", "[\"");
                                    value = value.Replace("]\"", "\"]");
                                }
                                else
                                {
                                    value = value.Replace("[", "[\"");
                                    value = value.Replace("]", "\"]");
                                }

                                value = value.Replace(",", "\",\"");
                                row[field] = value;
                            }
                            else
                            {
                                row[field] = rowdata;
                            }
                        }
                        //�������� �ᱻ�����string
                        else if (fieldType.Contains("["))
                        {
                            var value = rowdata.ToString();
                            value = value.Replace("\"[", "[");
                            value = value.Replace("]\"", "]");
                            row[field] = value;
                        }

                        else if (fieldType == "int" || fieldType == "float" || fieldType == "double")
                        {
                            var oldValue = rowdata.ToString();
                            if (fieldType == "int")
                            {
                                int value = 0;
                                if (int.TryParse(oldValue, out value))
                                {
                                    row[field] = value;
                                }
                                else
                                {
                                    row[field] = 0;
                                    Debug.LogErrorFormat("������ݳ���:{0}-{1}", i, j);
                                }
                            }
                            else if (fieldType == "float")
                            {
                                float value = 0;
                                if (float.TryParse(oldValue, out value))
                                {
                                    row[field] = value;
                                }
                                else
                                {
                                    row[field] = 0;
                                    Debug.LogErrorFormat("������ݳ���:{0}-{1}", i, j);
                                }
                            }
                            else if (fieldType == "double")
                            {
                                double value = 0;
                                if (double.TryParse(oldValue, out value))
                                {
                                    row[field] = value;
                                }
                                else
                                {
                                    row[field] = 0;
                                    Debug.LogErrorFormat("������ݳ���:{0}-{1}", i, j);
                                }
                            }
                        }
                        else if (field.Equals("string"))
                        {
                            row[field] = rowdata.ToString();
                        }
                        else
                        {
                            row[field] = rowdata;
                        }
                    }
                }

                //��ӵ���������
                if (row.Count > 0)
                {
                    table.Add(row);
                }
            }
            //����Json�ַ���
            string json = JsonMapper.ToJson(table,true);
            //�ѵ��ַ��������� ���´��������
            json = json.Replace("\"[", "[").Replace("]\"", "]");
            json = json.Replace("\\\"", "\"");
            json = json.Replace("\"\"\"\"", "\"\"");
            return json;
        }

        /// <summary>
        /// ��ȡһ������
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<object> GetRowDatas(int index)
        {
            List<object> list = new List<object>();

            //�ж�Excel�ļ����Ƿ�������ݱ�
            if (mResultSet.Tables.Count < 1)
            {
                return list;
            }

            //Ĭ�϶�ȡ��һ�����ݱ�
            DataTable mSheet = mResultSet.Tables[0];
            //�ж����ݱ����Ƿ��������
            if (mSheet.Rows.Count <= index)
            {
                return list;
            }

            //��ȡ����
            int colCount = mSheet.Columns.Count;
            for (int j = 0; j < colCount; j++)
            {
                object item = mSheet.Rows[index][j];
                list.Add(item);
            }


            return list;
        }

        /// <summary>
        /// ת��ΪCSV
        /// </summary>
        public void ConvertToCSV(string CSVPath, Encoding encoding)
        {
            //�ж�Excel�ļ����Ƿ�������ݱ�
            if (mResultSet.Tables.Count < 1) return;

            //Ĭ�϶�ȡ��һ�����ݱ�
            DataTable mSheet = mResultSet.Tables[0];

            //�ж����ݱ����Ƿ��������
            if (mSheet.Rows.Count < 1) return;

            //��ȡ���ݱ�����������
            int rowCount = mSheet.Rows.Count;
            int colCount = mSheet.Columns.Count;

            //����һ��StringBuilder�洢����
            StringBuilder stringBuilder = new StringBuilder();

            //��ȡ����
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    //ʹ��","�ָ�ÿһ����ֵ
                    stringBuilder.Append(mSheet.Rows[i][j] + ",");
                }

                //ʹ�û��з��ָ�ÿһ��
                stringBuilder.Append("\r\n");
            }

            //д���ļ�
            using (FileStream fileStream = new FileStream(CSVPath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, encoding))
                {
                    textWriter.Write(stringBuilder.ToString());
                }
            }
        }


        /// <summary>
        /// ת��Ϊlua
        /// </summary>
        /// <param name="luaPath">lua�ļ�·��</param>
        public void ConvertToLua(string luaPath, Encoding encoding)
        {
            //�ж�Excel�ļ����Ƿ�������ݱ�
            if (mResultSet.Tables.Count < 1)
                return;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("local datas = {");
            stringBuilder.Append("\r\n");

            //��ȡ���ݱ�
            foreach (DataTable mSheet in mResultSet.Tables)
            {
                //�ж����ݱ����Ƿ��������
                if (mSheet.Rows.Count < 1)
                    continue;

                //��ȡ���ݱ�����������
                int rowCount = mSheet.Rows.Count;
                int colCount = mSheet.Columns.Count;

                //׼��һ���б�洢�����������
                List<Dictionary<string, object>> table = new List<Dictionary<string, object>>();

                //��ȡ����
                for (int i = 1; i < rowCount; i++)
                {
                    //׼��һ���ֵ�洢ÿһ�е�����
                    Dictionary<string, object> row = new Dictionary<string, object>();
                    for (int j = 0; j < colCount; j++)
                    {
                        //��ȡ��1��������Ϊ��ͷ�ֶ�
                        string field = mSheet.Rows[0][j].ToString();
                        //Key-Value��Ӧ
                        row[field] = mSheet.Rows[i][j];
                    }
                    //��ӵ���������
                    table.Add(row);
                }
                stringBuilder.Append(string.Format("\t\"{0}\" = ", mSheet.TableName));
                stringBuilder.Append("{\r\n");
                foreach (Dictionary<string, object> dic in table)
                {
                    stringBuilder.Append("\t\t{\r\n");
                    foreach (string key in dic.Keys)
                    {
                        if (dic[key].GetType().Name == "String")
                            stringBuilder.Append(string.Format("\t\t\t\"{0}\" = \"{1}\",\r\n", key, dic[key]));
                        else
                            stringBuilder.Append(string.Format("\t\t\t\"{0}\" = {1},\r\n", key, dic[key]));
                    }
                    stringBuilder.Append("\t\t},\r\n");
                }
                stringBuilder.Append("\t}\r\n");
            }

            stringBuilder.Append("}\r\n");
            stringBuilder.Append("return datas");

            //д���ļ�
            using (FileStream fileStream = new FileStream(luaPath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, encoding))
                {
                    textWriter.Write(stringBuilder.ToString());
                }
            }
        }


        /// <summary>
        /// ����ΪXml
        /// </summary>
        public void ConvertToXml(string XmlFile)
        {
            //�ж�Excel�ļ����Ƿ�������ݱ�
            if (mResultSet.Tables.Count < 1) return;

            //Ĭ�϶�ȡ��һ�����ݱ�
            DataTable mSheet = mResultSet.Tables[0];

            //�ж����ݱ����Ƿ��������
            if (mSheet.Rows.Count < 1) return;

            //��ȡ���ݱ�����������
            int rowCount = mSheet.Rows.Count;
            int colCount = mSheet.Columns.Count;

            //����һ��StringBuilder�洢����
            StringBuilder stringBuilder = new StringBuilder();
            //����Xml�ļ�ͷ
            stringBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            stringBuilder.Append("\r\n");
            //�������ڵ�
            stringBuilder.Append("<Table>");
            stringBuilder.Append("\r\n");
            //��ȡ����
            for (int i = 1; i < rowCount; i++)
            {
                //�����ӽڵ�
                stringBuilder.Append("  <Row>");
                stringBuilder.Append("\r\n");
                for (int j = 0; j < colCount; j++)
                {
                    stringBuilder.Append("   <" + mSheet.Rows[0][j].ToString() + ">");
                    stringBuilder.Append(mSheet.Rows[i][j].ToString());
                    stringBuilder.Append("</" + mSheet.Rows[0][j].ToString() + ">");
                    stringBuilder.Append("\r\n");
                }

                //ʹ�û��з��ָ�ÿһ��
                stringBuilder.Append("  </Row>");
                stringBuilder.Append("\r\n");
            }

            //�պϱ�ǩ
            stringBuilder.Append("</Table>");
            //д���ļ�
            using (FileStream fileStream = new FileStream(XmlFile, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, Encoding.GetEncoding("utf-8")))
                {
                    textWriter.Write(stringBuilder.ToString());
                }
            }
        }

        /// <summary>
        /// ����Ŀ��ʵ��������
        /// </summary>
        private void SetTargetProperty(object target, string propertyName, object propertyValue)
        {
            //��ȡ����
            Type mType = target.GetType();
            //��ȡ���Լ���
            PropertyInfo[] mPropertys = mType.GetProperties();
            foreach (PropertyInfo property in mPropertys)
            {
                if (property.Name == propertyName)
                {
                    property.SetValue(target, Convert.ChangeType(propertyValue, property.PropertyType), null);
                }
            }
        }
    }
}