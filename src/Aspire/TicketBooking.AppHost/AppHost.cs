var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("ticketbooking");

builder.AddProject<Projects.TicketBooking_Api>("ticketbooking-api")
    .WithReference(database)
    .WaitFor(database);

builder.AddViteApp("public-web", "../../Frontend/public-web");
builder.AddViteApp("backoffice-web", "../../Frontend/backoffice-web");

builder.Build().Run();
