// TODO:
// - create table
// - add Constraints statemnets
// -- (not support constraints)
// ---- not null
// ---- unique
// ---- foreign key
//
// - etc
// -- rename filed?
// -- format SQL statement as you like :D

using System;
using System.Reflection;
using System.Collections.Generic;

namespace MZSQLGen {
    public interface ISQLGen {
        string GenStatement();
    }

    public interface IConstraint {
        string FieldName();

        string ConstraintStatement();
    }

    public class Constraint : IConstraint {
        string _fieldName = "";

        public Constraint(string fieldName) {
            _fieldName = fieldName;
        }

        public string FieldName() {
            return _fieldName;
        }

        public virtual string ConstraintStatement() {
            throw new NotImplementedException();
        }
    }

    public class Constraint_PrimaryKey : Constraint {
        public Constraint_PrimaryKey(string fieldName) : base(fieldName) {
        }

        public override string ConstraintStatement() {
            return "primary key";
        }
    }

    public class Constraint_Default : Constraint {
        string _valueString;

        public Constraint_Default(string fieldName, string valueString) : base(fieldName) {
            _valueString = valueString;
        }

        public override string ConstraintStatement() {
            return "default " + _valueString;
        }
    }

    public class Config {
        public delegate string ValueFormatFunc(object fieldValue ,string valueString);

        static Config _instance = null;
        Dictionary<Type,string> _defaultSQLTypesByType;
        Dictionary<Type,string> _customSQLTypesByType;
        Dictionary<Type,ValueFormatFunc> _defaultValueFormatFuncsByType;
        Dictionary<Type,ValueFormatFunc> _customValueFormatFuncsByType;

        static public Config Instance {
            get {
                if( _instance == null ) {
                    _instance = new Config();
                }

                return _instance;
            }
        }

        public string SQLTypeStringFromSysType(Type sysType) {
            if( _customSQLTypesByType != null && _customSQLTypesByType.ContainsKey( sysType ) == true ) {
                return _customSQLTypesByType[ sysType ];
            }

            if( _defaultSQLTypesByType == null ) {
                return null;
            }

            return ( _defaultSQLTypesByType.ContainsKey( sysType ) == true )? _defaultSQLTypesByType[ sysType ] : null;
        }

        public string SQLFormatedValueFromFieldInfo(FieldInfo fieldInfo, object objectHasValue) {
            Type type = fieldInfo.FieldType;
            object fieldValue = fieldInfo.GetValue( objectHasValue );
            string originValueString = fieldInfo.GetValue( objectHasValue ).ToString();

            if( _customValueFormatFuncsByType != null && _customValueFormatFuncsByType.ContainsKey( type ) == true ) {
                return _customValueFormatFuncsByType[ type ]( fieldValue, originValueString );
            }

            if( _defaultValueFormatFuncsByType.ContainsKey( type ) == true ) {
                return _defaultValueFormatFuncsByType[ type ]( fieldValue, originValueString );
            }

            return null;
        }

        public void ResetCustomType() {
            _customSQLTypesByType.Clear();
            _customValueFormatFuncsByType.Clear();
        }

        public void AddCustomTypeToSQLType(Type type, string sqlTypeString) {
            if( _customSQLTypesByType == null ) {
                _customSQLTypesByType = new Dictionary<Type, string>();
            }

            if( _customSQLTypesByType.ContainsKey( type ) == true ) {
                _customSQLTypesByType.Remove( type );
            }

            _customSQLTypesByType.Add( type, sqlTypeString );
        }

        public void AddCustomValueFormatFunc(Type type, ValueFormatFunc valueFormatFunc) {
            if( _customValueFormatFuncsByType == null ) {
                _customValueFormatFuncsByType = new Dictionary<Type, ValueFormatFunc>();
            }

            if( _customValueFormatFuncsByType.ContainsKey( type ) == true ) {
                _customValueFormatFuncsByType.Remove( type );
            }

            _customValueFormatFuncsByType.Add( type, valueFormatFunc );
        }

        Config() {
            // SQL Types
            _defaultSQLTypesByType = new Dictionary<Type, string>();
            _defaultSQLTypesByType.Add( typeof( string ), "text" );
            _defaultSQLTypesByType.Add( typeof( int ), "integer" );
            _defaultSQLTypesByType.Add( typeof( float ), "real" );

            // Format Funcs
            _defaultValueFormatFuncsByType = new Dictionary<Type, ValueFormatFunc>();
            _defaultValueFormatFuncsByType.Add( typeof( string ), (fieldValue, valueString) => {
                return "'" + valueString + "'";
            } );
            _defaultValueFormatFuncsByType.Add( typeof( int ), (fieldValue, valueString) => {
                return valueString;
            } );
            _defaultValueFormatFuncsByType.Add( typeof( float ), (fieldValue, valueString) => {
                return valueString;
            } );
        }
    }

    public class SQLCreateTableFromClass : ISQLGen {
        string _tableName;
        Type _classType;
        IConstraint[] _constraintsList;

        public SQLCreateTableFromClass(string tableName, Type classType) {
            _tableName = tableName;
            _classType = classType;
        }

        public SQLCreateTableFromClass(string tableName, Type classType, params IConstraint[] constraints) {
            _tableName = tableName;
            _classType = classType;
            _constraintsList = constraints;
        }

        public string GenStatement() {
            string sqlString = "create table " + _tableName + "( \n";
            foreach( FieldInfo fieldInfo in _classType.GetFields() ) {
                string fieldTypeString = Config.Instance.SQLTypeStringFromSysType( fieldInfo.FieldType );
                if( fieldTypeString == null || fieldTypeString == "" ) {
                    continue;
                }

                sqlString += "\t" + fieldInfo.Name + " " + fieldTypeString + ConstraintStatement( fieldInfo.Name ) + ",\n";
            }

            sqlString = sqlString.Remove( sqlString.LastIndexOf( "," ) );
            sqlString += "\n);";

            return sqlString;
        }

        string ConstraintStatement(string fieldName) {
            if( _constraintsList == null || _constraintsList.Length == 0 ) {
                return "";
            }

            string constraintStatement = "";
            foreach( IConstraint constraint in _constraintsList ) {
                if( constraint.FieldName().ToLower() == fieldName.ToLower() ) {
                    constraintStatement += " " + constraint.ConstraintStatement();
                }
            }

            return constraintStatement;
        }
    }

    public class SQLInsertIntoFromObject : ISQLGen {
        object _object;
        string _tableName;

        public SQLInsertIntoFromObject(object obj, string tableName) {
            _object = obj;
            _tableName = tableName;
        }

        public string GenStatement() {
            Type objType = _object.GetType();

            string insertsString = "insert into " + _tableName + " (\n";
            string valuesString = "values (\n";

            Action<FieldInfo, object> AddFieldStatementToInsertsAndValues = (FieldInfo fieldInfo, object targetObject) => {
                string fieldName = fieldInfo.Name;
                string valueString = Config.Instance.SQLFormatedValueFromFieldInfo( fieldInfo, _object );

                if( valueString == null || valueString == "" ) {
                    return;
                }

                insertsString += "\t" + fieldName + ",\n";
                valuesString += "\t" + valueString + ",\n";
            };

            foreach( FieldInfo fieldInfo in objType.GetFields() ) {
                AddFieldStatementToInsertsAndValues( fieldInfo, _object );
            }

            insertsString = insertsString.Remove( insertsString.LastIndexOf( "," ) );
            valuesString = valuesString.Remove( valuesString.LastIndexOf( "," ) );

            return insertsString + "\n) " + valuesString + "\n);";
        }
    }
}