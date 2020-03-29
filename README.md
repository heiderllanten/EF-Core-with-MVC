# EF-Core-with-MVC

## Data model

### Conventions

The amount of code you had to write in order for the Entity Framework to be able to create a complete database for you is minimal because of the use of conventions, or assumptions that the Entity Framework makes.

- The names of DbSet properties are used as table names. For entities not referenced by a DbSet property, entity class names are used as table names.
- Entity property names are used for column names.
- Entity properties that are named ID or classnameID are recognized as primary key properties.
- A property is interpreted as a foreign key property if it's named <navigation property name><primary key property name> (for example, StudentID for the Student navigation property since the Student entity's primary key is ID). Foreign key properties can also be named simply <primary key property name> (for example, EnrollmentID since the Enrollment entity's primary key is EnrollmentID).

Conventional behavior can be overridden. For example, you can explicitly specify table names, as you saw earlier in this tutorial. And you can set column names and set any property as primary key or foreign key, as you'll see in a later tutorial in this series.

### Asynchronous code

Asynchronous programming is the default mode for ASP.NET Core and EF Core.

A web server has a limited number of threads available, and in high load situations all of the available threads might be in use. When that happens, the server can't process new requests until the threads are freed up. With synchronous code, many threads may be tied up while they aren't actually doing any work because they're waiting for I/O to complete. With asynchronous code, when a process is waiting for I/O to complete, its thread is freed up for the server to use for processing other requests. As a result, asynchronous code enables server resources to be used more efficiently, and the server is enabled to handle more traffic without delays.

Asynchronous code does introduce a small amount of overhead at run time, but for low traffic situations the performance hit is negligible, while for high traffic situations, the potential performance improvement is substantial.

- The async keyword tells the compiler to generate callbacks for parts of the method body and to automatically create the Task<IActionResult> object that's returned.
- The return type Task<IActionResult> represents ongoing work with a result of type IActionResult.
- The await keyword causes the compiler to split the method into two parts. The first part ends with the operation that's started asynchronously. The second part is put into a callback method that's called when the operation completes.
- ToListAsync is the asynchronous version of the ToList extension method.

Some things to be aware of when you are writing asynchronous code that uses the Entity Framework:

- Only statements that cause queries or commands to be sent to the database are executed asynchronously. That includes, for example, ToListAsync, SingleOrDefaultAsync, and SaveChangesAsync. It doesn't include, for example, statements that just change an IQueryable, such as var students = context.Students.Where(s => s.LastName == "Davolio").
- An EF context isn't thread safe: don't try to do multiple operations in parallel. When you call any async EF method, always use the await keyword.
- If you want to take advantage of the performance benefits of async code, make sure that any library packages that you're using (such as for paging), also use async if they call any Entity Framework methods that cause queries to be sent to the database.

## Customize Pages

### Security note about overposting

The Bind attribute that the scaffolded code includes on the Create method is one way to protect against overposting in create scenarios. For example, suppose the Student entity includes a Secret property that you don't want this web page to set.

You can prevent overposting in edit scenarios by reading the entity from the database first and then calling TryUpdateModel, passing in an explicit allowed properties list. That's the method used in these tutorials.

An alternative way to prevent overposting that's preferred by many developers is to use view models rather than entity classes with model binding. Include only the properties you want to update in the view model. Once the MVC model binder has finished, copy the view model properties to the entity instance, optionally using a tool such as AutoMapper. Use _context.Entry on the entity instance to set its state to Unchanged, and then set Property("PropertyName").IsModified to true on each entity property that's included in the view model. This method works in both edit and create scenarios.

#### Recommended HttpPost Edit code: Read and update

These changes implement a security best practice to prevent overposting. The scaffolder generated a Bind attribute and added the entity created by the model binder to the entity set with a Modified flag. That code isn't recommended for many scenarios because the Bind attribute clears out any pre-existing data in fields not listed in the Include parameter.

The new code reads the existing entity and calls TryUpdateModel to update fields in the retrieved entity based on user input in the posted form data. The Entity Framework's automatic change tracking sets the Modified flag on the fields that are changed by form input. When the SaveChanges method is called, the Entity Framework creates SQL statements to update the database row. Concurrency conflicts are ignored, and only the table columns that were updated by the user are updated in the database. (A later tutorial shows how to handle concurrency conflicts.)

As a best practice to prevent overposting, the fields that you want to be updateable by the Edit page are whitelisted in the TryUpdateModel parameters. (The empty string preceding the list of fields in the parameter list is for a prefix to use with the form fields names.) Currently there are no extra fields that you're protecting, but listing the fields that you want the model binder to bind ensures that if you add fields to the data model in the future, they're automatically protected until you explicitly add them here.

#### Alternative HttpPost Edit code: Create and attach

The recommended HttpPost edit code ensures that only changed columns get updated and preserves data in properties that you don't want included for model binding. However, the read-first approach requires an extra database read, and can result in more complex code for handling concurrency conflicts. An alternative is to attach an entity created by the model binder to the EF context and mark it as modified. (Don't update your project with this code, it's only shown to illustrate an optional approach.)

### Entity States

The database context keeps track of whether entities in memory are in sync with their corresponding rows in the database, and this information determines what happens when you call the SaveChanges method. For example, when you pass a new entity to the Add method, that entity's state is set to Added. Then when you call the SaveChanges method, the database context issues a SQL INSERT command.

An entity may be in one of the following states:

- Added. The entity doesn't yet exist in the database. The SaveChanges method issues an INSERT statement.
- Unchanged. Nothing needs to be done with this entity by the SaveChanges method. When you read an entity from the database, the entity starts out with this status.
- Modified. Some or all of the entity's property values have been modified. The SaveChanges method issues an UPDATE statement.
- Deleted. The entity has been marked for deletion. The SaveChanges method issues a DELETE statement.
- Detached. The entity isn't being tracked by the database context.

In a desktop application, state changes are typically set automatically. You read an entity and make changes to some of its property values. This causes its entity state to automatically be changed to Modified. Then when you call SaveChanges, the Entity Framework generates a SQL UPDATE statement that updates only the actual properties that you changed.

In a web app, the DbContext that initially reads an entity and displays its data to be edited is disposed after a page is rendered. When the HttpPost Edit action method is called, a new web request is made and you have a new instance of the DbContext. If you re-read the entity in that new context, you simulate desktop processing.

But if you don't want to do the extra read operation, you have to use the entity object created by the model binder. The simplest way to do this is to set the entity state to Modified as is done in the alternative HttpPost Edit code shown earlier. Then when you call SaveChanges, the Entity Framework updates all columns of the database row, because the context has no way to know which properties you changed.

If you want to avoid the read-first approach, but you also want the SQL UPDATE statement to update only the fields that the user actually changed, the code is more complex. You have to save the original values in some way (such as by using hidden fields) so that they're available when the HttpPost Edit method is called. Then you can create a Student entity using the original values, call the Attach method with that original version of the entity, update the entity's values to the new values, and then call SaveChanges.

### Close database connections

To free up the resources that a database connection holds, the context instance must be disposed as soon as possible when you are done with it. The ASP.NET Core built-in dependency injection takes care of that task for you.

In Startup.cs, you call the AddDbContext extension method to provision the DbContext class in the ASP.NET Core DI container. That method sets the service lifetime to Scoped by default. Scoped means the context object lifetime coincides with the web request life time, and the Dispose method will be called automatically at the end of the web request.

### Handle transactions

By default the Entity Framework implicitly implements transactions. In scenarios where you make changes to multiple rows or tables and then call SaveChanges, the Entity Framework automatically makes sure that either all of your changes succeed or they all fail. If some changes are done first and then an error happens, those changes are automatically rolled back. For scenarios where you need more control -- for example, if you want to include operations done outside of Entity Framework in a transaction -- see Transactions.

### IQueryable Note

The method uses LINQ to Entities to specify the column to sort by. The code creates an IQueryable variable before the switch statement, modifies it in the switch statement, and calls the ToListAsync method after the switch statement. When you create and modify IQueryable variables, no query is sent to the database. The query isn't executed until you convert the IQueryable object into a collection by calling a method such as ToListAsync.

### Case sensitive note in IQueryable

Here you are calling the Where method on an IQueryable object, and the filter will be processed on the server. In some scenarios you might be calling the Where method as an extension method on an in-memory collection. (For example, suppose you change the reference to _context.Students so that instead of an EF DbSet it references a repository method that returns an IEnumerable collection.) The result would normally be the same but in some cases may be different.

For example, the .NET Framework implementation of the Contains method performs a case-sensitive comparison by default, but in SQL Server this is determined by the collation setting of the SQL Server instance. That setting defaults to case-insensitive. You could call the ToUpper method to make the test explicitly case-insensitive: Where(s => s.LastName.ToUpper().Contains(searchString.ToUpper()). That would ensure that results stay the same if you change the code later to use a repository which returns an IEnumerable collection instead of an IQueryable object. (When you call the Contains method on an IEnumerable collection, you get the .NET Framework implementation; when you call it on an IQueryable object, you get the database provider implementation.) However, there's a performance penalty for this solution. The ToUpper code would put a function in the WHERE clause of the TSQL SELECT statement. That would prevent the optimizer from using an index. Given that SQL is mostly installed as case-insensitive, it's best to avoid the ToUpper code until you migrate to a case-sensitive data store.

### <Form> tage helper note

This code uses the <form> tag helper to add the search text box and button. By default, the <form> tag helper submits form data with a POST, which means that parameters are passed in the HTTP message body and not in the URL as query strings. When you specify HTTP GET, the form data is passed in the URL as query strings, which enables users to bookmark the URL.

### Constructor note

Constructors can't run asynchronous code.

### Migration Notes

#### Create Initial Migration

Add-Migration InitialCreate
Update-Database

#### Remove Migration

Use the dotnet ef migrations remove command to remove a migration. dotnet ef migrations remove deletes the migration and ensures the snapshot is correctly reset. If dotnet ef migrations remove fails, use dotnet ef migrations remove -v to get more information on the failure.

#### Wording for Migrations

It's best to choose a word or phrase that summarizes what is being done in the migration. For example, you might name a later migration "AddDepartmentTable".

#### __EFMigrationsHistory Table

Inspect the database as you did in the first tutorial. You'll notice the addition of an __EFMigrationsHistory table that keeps track of which migrations have been applied to the database.

### Complex Data Model

#### DataType Attribute

- The DataType Enumeration provides for many data types, such as Date, Time, PhoneNumber, Currency, EmailAddress, and more. 
- The DataType attributes don't provide any validation.

The DataType attribute conveys the semantics of the data as opposed to how to render it on a screen, and provides the following benefits that you don't get with DisplayFormat:

- The browser can enable HTML5 features (for example to show a calendar control, the locale-appropriate currency symbol, email links, some client-side input validation, etc.).
- By default, the browser will render data using the correct format based on your locale.

[DataType(DataType.Date)]

#### DisplayFormat Attribute

The DisplayFormat attribute is used to explicitly specify the date format that will be displayed

The ApplyFormatInEditMode setting specifies that the formatting should also be applied when the value is displayed in a text box for editing. (You might not want that for some fields -- for example, for currency values, you might not want the currency symbol in the text box for editing.

You can use the DisplayFormat attribute by itself, but it's generally a good idea to use the DataType attribute also.

[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]

### StringLength Attribute

The StringLength attribute sets the maximum length in the database and provides client side and server side validation for ASP.NET Core MVC. You can also specify the minimum string length in this attribute, but the minimum value has no impact on the database schema.

- The MaxLength attribute provides functionality similar to the StringLength attribute but doesn't provide client side validation.

[StringLength(50)]

### RegularExpression Attribute

You can use the RegularExpression attribute to apply restrictions to the input. For example, the following code requires the first character to be upper case and the remaining characters to be alphabetical:

[RegularExpression(@"^[A-Z]+[a-zA-Z""'\s-]*$")]

#### Column Attribute 

- You can also use attributes to control how your classes and properties are mapped to the database.

    [Column("FirstName")]

- Earlier you used the Column attribute to change column name mapping. In the code for the Department entity, the Column attribute is being used to change SQL data type mapping so that the column will be defined using the SQL Server money type in the database:

    [Column(TypeName="money")]

#### Required Attribute

The Required attribute makes the name properties required fields. The Required attribute isn't needed for non-nullable types such as value types (DateTime, int, double, float, etc.). Types that can't be null are automatically treated as required fields.

The Required attribute must be used with MinimumLength for the MinimumLength to be enforced.

[Required]
[StringLength(50, MinimumLength=2)]

#### Display Attribute

The Display attribute specifies that the caption for the text boxes should be "First Name", "Last Name", "Full Name", and "Enrollment Date" instead of the property name in each instance (which has no space dividing the words).

[Display(Name = "Last Name")]

#### Key attribute

There's a one-to-zero-or-one relationship between the Instructor and the OfficeAssignment entities. An office assignment only exists in relation to the instructor it's assigned to, and therefore its primary key is also its foreign key to the Instructor entity. But the Entity Framework can't automatically recognize InstructorID as the primary key of this entity because its name doesn't follow the ID or classnameID naming convention. Therefore, the Key attribute is used to identify it as the key:

[Key]

#### Foreign Key Properties EF

The Entity Framework doesn't require you to add a foreign key property to your data model when you have a navigation property for a related entity. EF automatically creates foreign keys in the database wherever they're needed and creates shadow properties for them. But having the foreign key in the data model can make updates simpler and more efficient. For example, when you fetch a course entity to edit, the Department entity is null if you don't load it, so when you update the course entity, you would have to first fetch the Department entity. When the foreign key property DepartmentID is included in the data model, you don't need to fetch the Department entity before you update.

#### DatabaseGenerated attribute

The DatabaseGenerated attribute with the None parameter on the CourseID property specifies that primary key values are provided by the user rather than generated by the database.

[DatabaseGenerated(DatabaseGeneratedOption.None)]

#### Composite Key

The only way to identify composite primary keys to EF is by using the fluent API (it can't be done by using attributes).

modelBuilder.Entity<CourseAssignment>()
    .HasKey(c => new { c.CourseID, c.InstructorID });

#### Fluent API

You can also use the fluent API to specify most of the formatting, validation, and mapping rules that you can do by using attributes. Some attributes such as MinimumLength can't be applied with the fluent API. As mentioned previously, MinimumLength doesn't change the schema, it only applies a client and server side validation rule.

Some developers prefer to use the fluent API exclusively so that they can keep their entity classes "clean." You can mix attributes and fluent API if you want, and there are a few customizations that can only be done by using fluent API, but in general the recommended practice is to choose one of these two approaches and use that consistently as much as possible. If you do use both, note that wherever there's a conflict, Fluent API overrides attributes.

### How to load related data

#### Eager loading

- When the entity is read, related data is retrieved along with it. This typically results in a single join query that retrieves all of the data that's needed. You specify eager loading in Entity Framework Core by using the Include and ThenInclude methods.

- You can retrieve some of the data in separate queries, and EF "fixes up" the navigation properties. That is, EF automatically adds the separately retrieved entities where they belong in navigation properties of previously retrieved entities. For the query that retrieves related data, you can use the Load method instead of a method that returns a list or object, such as ToList or Single.

#### Explicit loading 

When the entity is first read, related data isn't retrieved. You write code that retrieves the related data if it's needed. As in the case of eager loading with separate queries, explicit loading results in multiple queries sent to the database. The difference is that with explicit loading, the code specifies the navigation properties to be loaded. In Entity Framework Core 1.1 you can use the Load method to do explicit loading.

#### Lazy loading 

When the entity is first read, related data isn't retrieved. However, the first time you attempt to access a navigation property, the data required for that navigation property is automatically retrieved. A query is sent to the database each time you try to get data from a navigation property for the first time. Entity Framework Core 1.0 doesn't support lazy loading.

#### Performance considerations loading related data

If you know you need related data for every entity retrieved, eager loading often offers the best performance, because a single query sent to the database is typically more efficient than separate queries for each entity retrieved. For example, suppose that each department has ten related courses. Eager loading of all related data would result in just a single (join) query and a single round trip to the database. A separate query for courses for each department would result in eleven round trips to the database. The extra round trips to the database are especially detrimental to performance when latency is high.

On the other hand, in some scenarios separate queries is more efficient. Eager loading of all related data in one query might cause a very complex join to be generated, which SQL Server can't process efficiently. Or if you need to access an entity's navigation properties only for a subset of a set of the entities you're processing, separate queries might perform better because eager loading of everything up front would retrieve more data than you need. If performance is critical, it's best to test performance both ways in order to make the best choice.

#### Single() method

You use the Single method on a collection when you know the collection will have only one item. The Single method throws an exception if the collection passed to it's empty or if there's more than one item. When you call the Single method, you can also pass in the Where condition instead of calling the Where method separately.

.Single(i => i.ID == id.Value)

#### SingleOrDefault() method

Returns a default value (null in this case) if the collection is empty.





### MVC

#### Controllers Note

Controllers shouldn't be overly complicated by too many responsibilities. To keep controller logic from becoming overly complex, push business logic out of the controller and into the domain model.

#### Filters Note

If you find that your controller actions frequently perform the same kinds of actions, move these common actions into filters.