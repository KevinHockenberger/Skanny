using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Skanny
{
  public class EnumBindingSourceExtension : MarkupExtension
  {
    private Type _enumType;
    public Type EnumType
    {
      get { return this._enumType; }
      set
      {
        if (value != this._enumType)
        {
          if (null != value)
          {
            Type enumType = Nullable.GetUnderlyingType(value) ?? value;
            if (!enumType.IsEnum) { throw new ArgumentException("Type must be for an Enum."); }
          }
          this._enumType = value;
        }
      }
    }
    public EnumBindingSourceExtension() { }
    public EnumBindingSourceExtension(Type enumType)
    {
      this.EnumType = enumType;
    }
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      if (null == this._enumType) { throw new InvalidOperationException("The EnumType must be specified."); }
      Type actualEnumType = Nullable.GetUnderlyingType(this._enumType) ?? this._enumType;
      Array enumValues = Enum.GetValues(actualEnumType);
      if (actualEnumType == this._enumType) { return enumValues; }
      Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
      enumValues.CopyTo(tempArray, 1);
      return tempArray;
    }
  }
  public class EnumDescriptionTypeConverter : EnumConverter
  {
    public EnumDescriptionTypeConverter(Type type) : base(type) { }
    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
      if (destinationType == typeof(string))
      {
        if (value != null)
        {
          System.Reflection.FieldInfo fi = value.GetType().GetField(value.ToString());
          if (fi != null)
          {
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
          }
        }
        return string.Empty;
      }
      return base.ConvertTo(context, culture, value, destinationType);
    }
  }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum AvailableThumbSizes
    {
      [Description("X-Small")]
      Xsmall = 50,
      [Description("Small")]
      Small = 100,
      [Description("Medium")]
      Medium = 250,
      [Description("Large")]
      Large = 500,
      [Description("X-Large")]
      Xlarge = 1000,
    }
  static class clsStatic
  {

  }
}
