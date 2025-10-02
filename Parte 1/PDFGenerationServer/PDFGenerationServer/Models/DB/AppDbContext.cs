using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PDFGenerationServer.Models.DB;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }

    public virtual DbSet<SalesOrderHeader> SalesOrderHeaders { get; set; }

    public virtual DbSet<SpecialOffer> SpecialOffers { get; set; }

    public virtual DbSet<SpecialOfferProduct> SpecialOfferProducts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK_Product_ProductID");

            entity.ToTable("Product", "Production", tb => tb.HasComment("Products sold or used in the manfacturing of sold products."));

            entity.HasIndex(e => e.Name, "AK_Product_Name").IsUnique();

            entity.HasIndex(e => e.ProductNumber, "AK_Product_ProductNumber").IsUnique();

            entity.HasIndex(e => e.Rowguid, "AK_Product_rowguid").IsUnique();

            entity.Property(e => e.ProductId)
                .HasComment("Primary key for Product records.")
                .HasColumnName("ProductID");
            entity.Property(e => e.Class)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasComment("H = High, M = Medium, L = Low");
            entity.Property(e => e.Color)
                .HasMaxLength(15)
                .HasComment("Product color.");
            entity.Property(e => e.DaysToManufacture).HasComment("Number of days required to manufacture the product.");
            entity.Property(e => e.DiscontinuedDate)
                .HasComment("Date the product was discontinued.")
                .HasColumnType("datetime");
            entity.Property(e => e.FinishedGoodsFlag)
                .HasDefaultValue(true)
                .HasComment("0 = Product is not a salable item. 1 = Product is salable.");
            entity.Property(e => e.ListPrice)
                .HasComment("Selling price.")
                .HasColumnType("money");
            entity.Property(e => e.MakeFlag)
                .HasDefaultValue(true)
                .HasComment("0 = Product is purchased, 1 = Product is manufactured in-house.");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Date and time the record was last updated.")
                .HasColumnType("datetime");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasComment("Name of the product.");
            entity.Property(e => e.ProductLine)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasComment("R = Road, M = Mountain, T = Touring, S = Standard");
            entity.Property(e => e.ProductModelId)
                .HasComment("Product is a member of this product model. Foreign key to ProductModel.ProductModelID.")
                .HasColumnName("ProductModelID");
            entity.Property(e => e.ProductNumber)
                .HasMaxLength(25)
                .HasComment("Unique product identification number.");
            entity.Property(e => e.ProductSubcategoryId)
                .HasComment("Product is a member of this product subcategory. Foreign key to ProductSubCategory.ProductSubCategoryID. ")
                .HasColumnName("ProductSubcategoryID");
            entity.Property(e => e.ReorderPoint).HasComment("Inventory level that triggers a purchase order or work order. ");
            entity.Property(e => e.Rowguid)
                .HasDefaultValueSql("(newid())")
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasColumnName("rowguid");
            entity.Property(e => e.SafetyStockLevel).HasComment("Minimum inventory quantity. ");
            entity.Property(e => e.SellEndDate)
                .HasComment("Date the product was no longer available for sale.")
                .HasColumnType("datetime");
            entity.Property(e => e.SellStartDate)
                .HasComment("Date the product was available for sale.")
                .HasColumnType("datetime");
            entity.Property(e => e.Size)
                .HasMaxLength(5)
                .HasComment("Product size.");
            entity.Property(e => e.SizeUnitMeasureCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Unit of measure for Size column.");
            entity.Property(e => e.StandardCost)
                .HasComment("Standard cost of the product.")
                .HasColumnType("money");
            entity.Property(e => e.Style)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasComment("W = Womens, M = Mens, U = Universal");
            entity.Property(e => e.Weight)
                .HasComment("Product weight.")
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.WeightUnitMeasureCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Unit of measure for Weight column.");
        });

        modelBuilder.Entity<SalesOrderDetail>(entity =>
        {
            entity.HasKey(e => new { e.SalesOrderId, e.SalesOrderDetailId }).HasName("PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID");

            entity.ToTable("SalesOrderDetail", "Sales", tb =>
                {
                    tb.HasComment("Individual products associated with a specific sales order. See SalesOrderHeader.");
                    tb.HasTrigger("iduSalesOrderDetail");
                });

            entity.HasIndex(e => e.Rowguid, "AK_SalesOrderDetail_rowguid").IsUnique();

            entity.HasIndex(e => e.ProductId, "IX_SalesOrderDetail_ProductID");

            entity.Property(e => e.SalesOrderId)
                .HasComment("Primary key. Foreign key to SalesOrderHeader.SalesOrderID.")
                .HasColumnName("SalesOrderID");
            entity.Property(e => e.SalesOrderDetailId)
                .ValueGeneratedOnAdd()
                .HasComment("Primary key. One incremental unique number per product sold.")
                .HasColumnName("SalesOrderDetailID");
            entity.Property(e => e.CarrierTrackingNumber)
                .HasMaxLength(25)
                .HasComment("Shipment tracking number supplied by the shipper.");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("(isnull(([UnitPrice]*((1.0)-[UnitPriceDiscount]))*[OrderQty],(0.0)))", false)
                .HasComment("Per product subtotal. Computed as UnitPrice * (1 - UnitPriceDiscount) * OrderQty.")
                .HasColumnType("numeric(38, 6)");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Date and time the record was last updated.")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderQty).HasComment("Quantity ordered per product.");
            entity.Property(e => e.ProductId)
                .HasComment("Product sold to customer. Foreign key to Product.ProductID.")
                .HasColumnName("ProductID");
            entity.Property(e => e.Rowguid)
                .HasDefaultValueSql("(newid())")
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasColumnName("rowguid");
            entity.Property(e => e.SpecialOfferId)
                .HasComment("Promotional code. Foreign key to SpecialOffer.SpecialOfferID.")
                .HasColumnName("SpecialOfferID");
            entity.Property(e => e.UnitPrice)
                .HasComment("Selling price of a single product.")
                .HasColumnType("money");
            entity.Property(e => e.UnitPriceDiscount)
                .HasDefaultValueSql("((0.0))")
                .HasComment("Discount amount.")
                .HasColumnType("money");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderDetails).HasForeignKey(d => d.SalesOrderId);

            entity.HasOne(d => d.SpecialOfferProduct).WithMany(p => p.SalesOrderDetails)
                .HasForeignKey(d => new { d.SpecialOfferId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrderDetail_SpecialOfferProduct_SpecialOfferIDProductID");
        });

        modelBuilder.Entity<SalesOrderHeader>(entity =>
        {
            entity.HasKey(e => e.SalesOrderId).HasName("PK_SalesOrderHeader_SalesOrderID");

            entity.ToTable("SalesOrderHeader", "Sales", tb =>
                {
                    tb.HasComment("General sales order information.");
                    tb.HasTrigger("uSalesOrderHeader");
                });

            entity.HasIndex(e => e.SalesOrderNumber, "AK_SalesOrderHeader_SalesOrderNumber").IsUnique();

            entity.HasIndex(e => e.Rowguid, "AK_SalesOrderHeader_rowguid").IsUnique();

            entity.HasIndex(e => e.CustomerId, "IX_SalesOrderHeader_CustomerID");

            entity.HasIndex(e => e.SalesPersonId, "IX_SalesOrderHeader_SalesPersonID");

            entity.Property(e => e.SalesOrderId)
                .HasComment("Primary key.")
                .HasColumnName("SalesOrderID");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(15)
                .HasComment("Financial accounting number reference.");
            entity.Property(e => e.BillToAddressId)
                .HasComment("Customer billing address. Foreign key to Address.AddressID.")
                .HasColumnName("BillToAddressID");
            entity.Property(e => e.Comment)
                .HasMaxLength(128)
                .HasComment("Sales representative comments.");
            entity.Property(e => e.CreditCardApprovalCode)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasComment("Approval code provided by the credit card company.");
            entity.Property(e => e.CreditCardId)
                .HasComment("Credit card identification number. Foreign key to CreditCard.CreditCardID.")
                .HasColumnName("CreditCardID");
            entity.Property(e => e.CurrencyRateId)
                .HasComment("Currency exchange rate used. Foreign key to CurrencyRate.CurrencyRateID.")
                .HasColumnName("CurrencyRateID");
            entity.Property(e => e.CustomerId)
                .HasComment("Customer identification number. Foreign key to Customer.BusinessEntityID.")
                .HasColumnName("CustomerID");
            entity.Property(e => e.DueDate)
                .HasComment("Date the order is due to the customer.")
                .HasColumnType("datetime");
            entity.Property(e => e.Freight)
                .HasDefaultValueSql("((0.00))")
                .HasComment("Shipping cost.")
                .HasColumnType("money");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Date and time the record was last updated.")
                .HasColumnType("datetime");
            entity.Property(e => e.OnlineOrderFlag)
                .HasDefaultValue(true)
                .HasComment("0 = Order placed by sales person. 1 = Order placed online by customer.");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Dates the sales order was created.")
                .HasColumnType("datetime");
            entity.Property(e => e.PurchaseOrderNumber)
                .HasMaxLength(25)
                .HasComment("Customer purchase order number reference. ");
            entity.Property(e => e.RevisionNumber).HasComment("Incremental number to track changes to the sales order over time.");
            entity.Property(e => e.Rowguid)
                .HasDefaultValueSql("(newid())")
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasColumnName("rowguid");
            entity.Property(e => e.SalesOrderNumber)
                .HasMaxLength(25)
                .HasComputedColumnSql("(isnull(N'SO'+CONVERT([nvarchar](23),[SalesOrderID]),N'*** ERROR ***'))", false)
                .HasComment("Unique sales order identification number.");
            entity.Property(e => e.SalesPersonId)
                .HasComment("Sales person who created the sales order. Foreign key to SalesPerson.BusinessEntityID.")
                .HasColumnName("SalesPersonID");
            entity.Property(e => e.ShipDate)
                .HasComment("Date the order was shipped to the customer.")
                .HasColumnType("datetime");
            entity.Property(e => e.ShipMethodId)
                .HasComment("Shipping method. Foreign key to ShipMethod.ShipMethodID.")
                .HasColumnName("ShipMethodID");
            entity.Property(e => e.ShipToAddressId)
                .HasComment("Customer shipping address. Foreign key to Address.AddressID.")
                .HasColumnName("ShipToAddressID");
            entity.Property(e => e.Status)
                .HasDefaultValue((byte)1)
                .HasComment("Order current status. 1 = In process; 2 = Approved; 3 = Backordered; 4 = Rejected; 5 = Shipped; 6 = Cancelled");
            entity.Property(e => e.SubTotal)
                .HasDefaultValueSql("((0.00))")
                .HasComment("Sales subtotal. Computed as SUM(SalesOrderDetail.LineTotal)for the appropriate SalesOrderID.")
                .HasColumnType("money");
            entity.Property(e => e.TaxAmt)
                .HasDefaultValueSql("((0.00))")
                .HasComment("Tax amount.")
                .HasColumnType("money");
            entity.Property(e => e.TerritoryId)
                .HasComment("Territory in which the sale was made. Foreign key to SalesTerritory.SalesTerritoryID.")
                .HasColumnName("TerritoryID");
            entity.Property(e => e.TotalDue)
                .HasComputedColumnSql("(isnull(([SubTotal]+[TaxAmt])+[Freight],(0)))", false)
                .HasComment("Total due from customer. Computed as Subtotal + TaxAmt + Freight.")
                .HasColumnType("money");
        });

        modelBuilder.Entity<SpecialOffer>(entity =>
        {
            entity.HasKey(e => e.SpecialOfferId).HasName("PK_SpecialOffer_SpecialOfferID");

            entity.ToTable("SpecialOffer", "Sales", tb => tb.HasComment("Sale discounts lookup table."));

            entity.HasIndex(e => e.Rowguid, "AK_SpecialOffer_rowguid").IsUnique();

            entity.Property(e => e.SpecialOfferId)
                .HasComment("Primary key for SpecialOffer records.")
                .HasColumnName("SpecialOfferID");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasComment("Group the discount applies to such as Reseller or Customer.");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasComment("Discount description.");
            entity.Property(e => e.DiscountPct)
                .HasDefaultValueSql("((0.00))")
                .HasComment("Discount precentage.")
                .HasColumnType("smallmoney");
            entity.Property(e => e.EndDate)
                .HasComment("Discount end date.")
                .HasColumnType("datetime");
            entity.Property(e => e.MaxQty).HasComment("Maximum discount percent allowed.");
            entity.Property(e => e.MinQty).HasComment("Minimum discount percent allowed.");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Date and time the record was last updated.")
                .HasColumnType("datetime");
            entity.Property(e => e.Rowguid)
                .HasDefaultValueSql("(newid())")
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasColumnName("rowguid");
            entity.Property(e => e.StartDate)
                .HasComment("Discount start date.")
                .HasColumnType("datetime");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasComment("Discount type category.");
        });

        modelBuilder.Entity<SpecialOfferProduct>(entity =>
        {
            entity.HasKey(e => new { e.SpecialOfferId, e.ProductId }).HasName("PK_SpecialOfferProduct_SpecialOfferID_ProductID");

            entity.ToTable("SpecialOfferProduct", "Sales", tb => tb.HasComment("Cross-reference table mapping products to special offer discounts."));

            entity.HasIndex(e => e.Rowguid, "AK_SpecialOfferProduct_rowguid").IsUnique();

            entity.HasIndex(e => e.ProductId, "IX_SpecialOfferProduct_ProductID");

            entity.Property(e => e.SpecialOfferId)
                .HasComment("Primary key for SpecialOfferProduct records.")
                .HasColumnName("SpecialOfferID");
            entity.Property(e => e.ProductId)
                .HasComment("Product identification number. Foreign key to Product.ProductID.")
                .HasColumnName("ProductID");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasComment("Date and time the record was last updated.")
                .HasColumnType("datetime");
            entity.Property(e => e.Rowguid)
                .HasDefaultValueSql("(newid())")
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasColumnName("rowguid");

            entity.HasOne(d => d.Product).WithMany(p => p.SpecialOfferProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SpecialOffer).WithMany(p => p.SpecialOfferProducts)
                .HasForeignKey(d => d.SpecialOfferId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
