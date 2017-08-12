using System;
using Jil;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions
#else
namespace System.Linq.CommonTools
#endif
{
    /// <summary>
    /// Utility structure for storing complex types as JSON strings in DB table.
    /// </summary>
    public struct JsonField<TObject>
        where TObject : class
    {
        private TObject _object;
        private string _json;
        private bool _isMaterialized;
        private bool _hasDefault;

        public string Json
        {
            get
            {
                if (_isMaterialized)
                {
                    _json = _object == null
                        ? null : JSON.Serialize(_object, Options.IncludeInherited);
                }
                return _json;
            }
            set
            {
                _json = value;
                _isMaterialized = false;
            }
        }

        public TObject Object
        {
            get
            {
                if (!_isMaterialized)
                {
                    if (String.IsNullOrEmpty(_json) || _json == "null")
                    {
                        if (_hasDefault)
                        {
                            _hasDefault = false;
                        }
                        else
                        {
                            _object = null;
                        }
                    }
                    else
                    {
                        _object = JSON.Deserialize<TObject>(_json);
                    }
                    _isMaterialized = true;
                }
                return _object;
            }
            set
            {
                _object = value;
                _isMaterialized = true;
            }
        }

        public static implicit operator JsonField<TObject>(TObject defaultValue)
        {
            var field = new JsonField<TObject>();

            field._object = defaultValue;
            field._hasDefault = true;

            return field;
        }
    }
}
