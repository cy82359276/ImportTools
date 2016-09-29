﻿//------------------------------------------------------------------------------
// <auto-generated>
//    此代码是根据模板生成的。
//
//    手动更改此文件可能会导致应用程序中发生异常行为。
//    如果重新生成代码，则将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace TheDataResourceImporter
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class DataSourceEntities : DbContext
    {
        public DataSourceEntities()
            : base("name=DataSourceEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<IMPORT_ERROR> IMPORT_ERROR { get; set; }
        public DbSet<IMPORT_SESSION> IMPORT_SESSION { get; set; }
        public DbSet<S_AMERICAN_BRAND_GRAPHCLASSIFY> S_AMERICAN_BRAND_GRAPHCLASSIFY { get; set; }
        public DbSet<S_AMERICAN_BRAND_USCLASSIFY> S_AMERICAN_BRAND_USCLASSIFY { get; set; }
        public DbSet<S_AMERICAN_DESIGN_PATENT> S_AMERICAN_DESIGN_PATENT { get; set; }
        public DbSet<S_AUSTRALIAN_PATENT_FULLTEXT> S_AUSTRALIAN_PATENT_FULLTEXT { get; set; }
        public DbSet<S_AUSTRIA_PATENT_FULLTEXT> S_AUSTRIA_PATENT_FULLTEXT { get; set; }
        public DbSet<S_BELGIAN_PATENT_FULLTEXT> S_BELGIAN_PATENT_FULLTEXT { get; set; }
        public DbSet<S_BRITISH_PATENT_FULLTEXTCODE> S_BRITISH_PATENT_FULLTEXTCODE { get; set; }
        public DbSet<S_CANADIAN_PATENT_DESCRIPTION> S_CANADIAN_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_CHINA_BIOLOGICAL_PROCESS> S_CHINA_BIOLOGICAL_PROCESS { get; set; }
        public DbSet<S_CHINA_BOOK> S_CHINA_BOOK { get; set; }
        public DbSet<S_CHINA_BRAND> S_CHINA_BRAND { get; set; }
        public DbSet<S_CHINA_BRAND_CLASSIFICATION> S_CHINA_BRAND_CLASSIFICATION { get; set; }
        public DbSet<S_CHINA_CIRCUITLAYOUT> S_CHINA_CIRCUITLAYOUT { get; set; }
        public DbSet<S_CHINA_COURTCASE_PROCESS> S_CHINA_COURTCASE_PROCESS { get; set; }
        public DbSet<S_CHINA_CUSTOMS_RECORD> S_CHINA_CUSTOMS_RECORD { get; set; }
        public DbSet<S_CHINA_LAWSTATE_INDEXINGLIB> S_CHINA_LAWSTATE_INDEXINGLIB { get; set; }
        public DbSet<S_CHINA_MEDICINE_PATENT_HANDLE> S_CHINA_MEDICINE_PATENT_HANDLE { get; set; }
        public DbSet<S_CHINA_MEDICINE_PATENT_TRANS> S_CHINA_MEDICINE_PATENT_TRANS { get; set; }
        public DbSet<S_CHINA_PATENT_ABSTRACTS> S_CHINA_PATENT_ABSTRACTS { get; set; }
        public DbSet<S_CHINA_PATENT_BIBLIOGRAPHIC> S_CHINA_PATENT_BIBLIOGRAPHIC { get; set; }
        public DbSet<S_CHINA_PATENT_BIOLOGICALSEQ> S_CHINA_PATENT_BIOLOGICALSEQ { get; set; }
        public DbSet<S_CHINA_PATENT_FEEINFORMATION> S_CHINA_PATENT_FEEINFORMATION { get; set; }
        public DbSet<S_CHINA_PATENT_FULLTEXT_PDF> S_CHINA_PATENT_FULLTEXT_PDF { get; set; }
        public DbSet<S_CHINA_PATENT_GAZETTE> S_CHINA_PATENT_GAZETTE { get; set; }
        public DbSet<S_CHINA_PATENT_INVALID> S_CHINA_PATENT_INVALID { get; set; }
        public DbSet<S_CHINA_PATENT_LAWSTATE> S_CHINA_PATENT_LAWSTATE { get; set; }
        public DbSet<S_CHINA_PATENT_LAWSTATE_CHANGE> S_CHINA_PATENT_LAWSTATE_CHANGE { get; set; }
        public DbSet<S_CHINA_PATENT_NOTICES> S_CHINA_PATENT_NOTICES { get; set; }
        public DbSet<S_CHINA_PATENT_STAND_TEXTIMAGE> S_CHINA_PATENT_STAND_TEXTIMAGE { get; set; }
        public DbSet<S_CHINA_PATENT_STANDARDFULLTXT> S_CHINA_PATENT_STANDARDFULLTXT { get; set; }
        public DbSet<S_CHINA_PATENT_TEXTCODE> S_CHINA_PATENT_TEXTCODE { get; set; }
        public DbSet<S_CHINA_PATENT_TEXTIMAGE> S_CHINA_PATENT_TEXTIMAGE { get; set; }
        public DbSet<S_CHINA_PHARMACEUTICAL_PATENT> S_CHINA_PHARMACEUTICAL_PATENT { get; set; }
        public DbSet<S_CHINA_STANDARD_SIMPCITATION> S_CHINA_STANDARD_SIMPCITATION { get; set; }
        public DbSet<S_COMMUNITY_INTELLECTUALRECORD> S_COMMUNITY_INTELLECTUALRECORD { get; set; }
        public DbSet<S_COMPANY_CODE_LIBRARY> S_COMPANY_CODE_LIBRARY { get; set; }
        public DbSet<S_DATA_RESOURCE_TYPES_DETAIL> S_DATA_RESOURCE_TYPES_DETAIL { get; set; }
        public DbSet<S_DOCDB> S_DOCDB { get; set; }
        public DbSet<S_EURASIAN_PATENT_DESCRIPTION> S_EURASIAN_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_EUROPEAN_PATENT_FULLTEXT> S_EUROPEAN_PATENT_FULLTEXT { get; set; }
        public DbSet<S_FOREIGN_PATENT_FULLTEXT_PDF> S_FOREIGN_PATENT_FULLTEXT_PDF { get; set; }
        public DbSet<S_FRENCH_DESIGN_PATENT> S_FRENCH_DESIGN_PATENT { get; set; }
        public DbSet<S_FRENCH_PATENT_DESCRIPTION> S_FRENCH_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_GERMAN_DESIGN_PATENT> S_GERMAN_DESIGN_PATENT { get; set; }
        public DbSet<S_GERMAN_PATENT_DESCRIPTION> S_GERMAN_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_GLOBAL_PATENT_CITATION> S_GLOBAL_PATENT_CITATION { get; set; }
        public DbSet<S_HONGKONG_PATENT_DESCRIPTION> S_HONGKONG_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_IMPORT_BATH> S_IMPORT_BATH { get; set; }
        public DbSet<S_ISRAEL_PATENT_DESCRIPTION> S_ISRAEL_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_JAPAN_DESIGN_PATENT> S_JAPAN_DESIGN_PATENT { get; set; }
        public DbSet<S_JAPAN_PATENT_ABSTRACTS> S_JAPAN_PATENT_ABSTRACTS { get; set; }
        public DbSet<S_JAPAN_PATENT_FULLTEXTCODE> S_JAPAN_PATENT_FULLTEXTCODE { get; set; }
        public DbSet<S_KOREA_DESIGN_PATENT> S_KOREA_DESIGN_PATENT { get; set; }
        public DbSet<S_KOREA_PATENT_ABSTRACTS> S_KOREA_PATENT_ABSTRACTS { get; set; }
        public DbSet<S_KOREAN_PATENT_FULLTEXTCODE> S_KOREAN_PATENT_FULLTEXTCODE { get; set; }
        public DbSet<S_MACAO_PATENT_DESCRIPTION> S_MACAO_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_PATENT_FAMILY> S_PATENT_FAMILY { get; set; }
        public DbSet<S_PATENT_PAYMENT> S_PATENT_PAYMENT { get; set; }
        public DbSet<S_POLAND_PATENT_DESCRIPTION> S_POLAND_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_RUSSIAN_DESIGN_PATENT> S_RUSSIAN_DESIGN_PATENT { get; set; }
        public DbSet<S_RUSSIAN_PATENT_ABSTRACTS> S_RUSSIAN_PATENT_ABSTRACTS { get; set; }
        public DbSet<S_RUSSIAN_PATENT_DESCRIPTION> S_RUSSIAN_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_SINGAPORE_PATENT_DESCRIPTION> S_SINGAPORE_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_SPANISH_PATENT_FULLTEXT> S_SPANISH_PATENT_FULLTEXT { get; set; }
        public DbSet<S_SWISS_PATENT_FULLTEXTCODE> S_SWISS_PATENT_FULLTEXTCODE { get; set; }
        public DbSet<S_TAIWAN_PATENT_DESCRIPTION> S_TAIWAN_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_WIPO_PATENT_DESCRIPTION> S_WIPO_PATENT_DESCRIPTION { get; set; }
        public DbSet<S_WORLD_LEGAL_STATUS> S_WORLD_LEGAL_STATUS { get; set; }
        public DbSet<S_ZONE_CODE_BASE> S_ZONE_CODE_BASE { get; set; }
        public DbSet<S_AMERICA_APPLY_BRAND> S_AMERICA_APPLY_BRAND { get; set; }
        public DbSet<S_AMERICA_TRANSFER_BRAND> S_AMERICA_TRANSFER_BRAND { get; set; }
        public DbSet<S_AMERICA_TRIAL_BRAND> S_AMERICA_TRIAL_BRAND { get; set; }
        public DbSet<S_CHINA_BRAND_LICENSE> S_CHINA_BRAND_LICENSE { get; set; }
        public DbSet<S_CHINA_BRAND_TRANSFER> S_CHINA_BRAND_TRANSFER { get; set; }
        public DbSet<S_CHINA_WELLKNOWN_BRAND> S_CHINA_WELLKNOWN_BRAND { get; set; }
        public DbSet<S_MADRID_BRAND_ENTER_CHINA> S_MADRID_BRAND_ENTER_CHINA { get; set; }
        public DbSet<S_MADRID_BRAND_PURCHASE> S_MADRID_BRAND_PURCHASE { get; set; }
        public DbSet<S_CHINA_PATENT_REVIEW> S_CHINA_PATENT_REVIEW { get; set; }
        public DbSet<S_CHINA_PATENT_JUDGMENT> S_CHINA_PATENT_JUDGMENT { get; set; }
        public DbSet<S_JOURNAL_PROJECT_ABSTRACT> S_JOURNAL_PROJECT_ABSTRACT { get; set; }
        public DbSet<S_CHINA_PATENT_LAWSPROCESS> S_CHINA_PATENT_LAWSPROCESS { get; set; }
        public DbSet<S_BIOLOGICAL_SEQ> S_BIOLOGICAL_SEQ { get; set; }
        public DbSet<S_FOREIGN_PATENT_SEQUENCE> S_FOREIGN_PATENT_SEQUENCE { get; set; }
        public DbSet<S_AMERICAN_PATENT_FULLTEXT> S_AMERICAN_PATENT_FULLTEXT { get; set; }
    }
}
