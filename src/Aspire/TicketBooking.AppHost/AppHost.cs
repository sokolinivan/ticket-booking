var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TicketBooking_Api>("ticketbooking-api");

builder.AddViteApp("public-web", "../../Frontend/public-web");
builder.AddViteApp("backoffice-web", "../../Frontend/backoffice-web");

builder.Build().Run();
