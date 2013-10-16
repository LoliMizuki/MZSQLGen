# MZSQLGen #

This sample code block can help you gen SQL statement (`CREATE TABLE` or `INSERT INTO` ... ) from c# classes or instance quickly.

## Usage
If has a class define like below:

```c#
    public class MyClass {
        public int ID;
        public string StringField;
        public float FloatField;
    }
```    
    
We can quick gen it's `CREATE TABLE` statement used:

```c#
    string tableName = "MyClass";
    SQLCreateTableFromClass createTableStatement = new SQLCreateTableFromClass( 
        tableName,
        typeof( MyClass ),
        // add Primary Key Constraint
        new Constraint_PrimaryKey( "ID" ),                           
        // add Default Constraint 
        new Constraint_Default( "StringField", "'hello world'" )       
    );
```
                
Then use method: 

```c#    
    createTableStatement.GenStatement();
```    
    
Would gen SQL statement:
    
```sql
    create table MyClass( 
        ID integer primary key,
        StringField text default 'hello world',
        FloatField real
    );
```
    
If have instace of MyClass:

```c#
    MyClass myObject = new MyClass();
    myObject.ID = 1;
    myObject.StringField = "hello c_sharp";
    myObject.FloatField = 3.14;
```

We can use `SQLInsertIntoFromObject` to gen `INSERT INTO` statement:

```c#   
    string tableName = "MyClass";
    SQLInsertIntoFromObject insertIntoStatement = new SQLInsertIntoFromObject( myObject, tableName );
    insertIntoStatement.GenStatement();
```

Output:
   
```sql   
    insert into MyClass (
        ID,
        StringField,
        FloatField
    ) values (
        1,
        'hello c_sharp',
        3.14
    );
```
    
## Customize Type
If you have any type that not define in system, you can add it to singleton class `MZSQLGen.SQLConfig` that your system/project can use it.

As example, want to support type `Vector3`(DirectX, Unity ... ) to SQL `text`, we can:

```c#    
    SQLConfig.Instance.AddCustomTypeToSQLType( typeof( Vector3 ), "text" );
```    

And set it's value format function:

```c#
    SQLConfig.Instance.AddCustomValueFormatFunc( 
        typeof( Vector3 ),
        (fieldValue, valueString) => {
            return "'" + valueString + "'";
        } 
    );
```
    
Why use delegate function to formated? because sometime we need do more complex process to our value. as example, we want to store List<Vector2> as text to database, so can add new func to SQLConfig:

```c#    
    SQLConfig.Instance.AddCustomValueFormatFunc( 
        typeof( List<Vector2> ),
        (fieldValue, valueString) => { 
                List<Vector2> positionsList = (List<Vector2>)fieldValue;
                string listString = "";
    
                foreach( Vector2 pos in positionsList ) {
                    listString += pos.ToString() + ",";
                }
                listString = listString.Remove( listString.LastIndexOf( "," ) );
    
                return "'" + listString + "'";
        }
    );
```
    
## Support System Type to SQLite3 Type
- `stirng`  -> `text`
- `int`     -> `integer`
- `float`   -> `real`

## Support Constraints
- `PrimaryKey`
- `Default`

## Limitations
This repo has many limitation now, as
- Parse `FIELD TYPE` ONLY
    - Can not gen statements of `Properties` or `Class Methods`.

## Quick References

### interface
- `ISQLGen`: Define SQLGen implementions.
- `IConstraint`: Define constraint implementions. 

### classes
`SQLConfig`: Singleton class
`SQLCreateTableFromClass`
`SQLInsertIntoFromObject`
`Constraint`
`Constraint_PrimaryKey`
`Constraint_Default`

## Feture Work
- More SQL statement support(`ALTER TABLE`, ... ).
- More constraint support(`not null`, `unique`, `foreign key`, ... ).
- Customize gen statement format.
- Rename field as required.
- Unit test.
