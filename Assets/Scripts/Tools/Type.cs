/// <summary>
/// 创建者：长生
/// 时间：2021年11月20日10:32:26
/// 功能：枚举
/// </summary>
namespace GameLogic
{
    /// <summary>
    /// 多语言
    /// </summary>
    public enum Multilingual
    {
        //中文
        ZH = 0,
        //英文
        EN,
    }

    /// <summary>
    /// 资源加载路径
    /// </summary>
    public enum AssetLoadPath
    {
        Editor = 0,
        Persistent,
        StreamingAsset,
        EditorLibrary
    }

    /// <summary>
    /// 比如在rpg游戏中，标识当前界面是否会挡住主界面
    /// </summary>
    public enum PanelSize
    {
        //非全屏界面，不会挡住主界面，比如穿戴提示，活动推送，领取红包
        SmallPanel,
        //遮挡了主界面的80%，上下和两边的空隙可利用截屏一张图后，就可以隐藏Main Camera
        SinglePanel,
        //处于全屏界面，可以禁用Main Camera，减少开销
        FullScreen,
    }

    /// <summary>
    /// 字段类型
    /// </summary>
    public enum FieldTypes
    {
        //未知类型
        Unknown,
        //未知类型表
        UnknownList,    
        Bool,
        Int,
        //Int数组
        Ints,          
        Float,
        //Float数组
        Floats,         
        Long,
        //Long数组
        Longs,          
        Vector2,
        Vector3,
        Vector4,
        //矩阵
        Rect,
        //颜色
        Color,          
        String,
        Strings,
        //自定义类型
        CustomType,
        //自定义类型数组
        CustomTypeList
    }

    public enum UnityLayerDef
    {
        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,

        Water = 4,
        UI = 5,

        //以下为自定义
        //Hidden = 8,
    }

    //文字大小
    public enum FontSize
    {
        Headline,           //大标题
        MediumTitle,        //中等标题
        Subtitle,           //小标题
        LargeButton,        //大按钮
        MediumButton,       //中等按钮
        SmallButton,        //小按钮
        UltraSmallButton,   //超小按钮
        BigContent,         //大内容
        MediumContent,      //中等内容
        SmallContent,       //小内容
        UltraSmallContent,  //超小内容
    }
}

namespace GameLogic.Json
{
    public enum JsonType
    {
        None,
        Object,
        Array,
        String,
        Int,
        Long,
        Double,
        Boolean
    }

    public enum JsonToken
    {
        None,

        ObjectStart,
        PropertyName,
        ObjectEnd,

        ArrayStart,
        ArrayEnd,

        Int,
        Long,
        Double,

        String,

        Boolean,
        Null
    }
}