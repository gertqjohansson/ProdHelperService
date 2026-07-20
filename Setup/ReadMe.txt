ProdHelperService has been installed and registered as a Windows Service
(Service name: ProdHelperService), but it has NOT been started yet.

Before starting it, edit appsettings.json in the install folder and fill in
the values currently marked "<SET-ME>":

  - ConnectionStrings:ProdHelperDb  (production database connection string)
  - Relay:Key                      (Azure Relay shared access key)
  - Jwt:Key                        (JWT signing key)
  - Email:ConnectionString         (Azure Communication Email connection string,
                                     if email sending is used)

Once configured, start the service with:

  net start ProdHelperService

or via Services.msc / ProdHelperService.AdminApp.
