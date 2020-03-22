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