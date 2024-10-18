using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QDM_PartNoSearch.Models;

public partial class Flavor2Context : DbContext
{
    public Flavor2Context()
    {
    }

    public Flavor2Context(DbContextOptions<Flavor2Context> options)
        : base(options)
    {
    }
    public virtual DbSet<Invmb> Invmbs { get; set; }
    public virtual DbSet<Invmc> Invmcs { get; set; }
    public virtual DbSet<Invmh> Invmhs { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Chinese_Taiwan_Stroke_BIN");

        modelBuilder.Entity<Invmb>(entity =>
        {
            entity.HasKey(e => e.Mb001);

            entity.ToTable("INVMB");

            entity.HasIndex(e => e.Mb026, "INVMB_K01");

            entity.HasIndex(e => e.Mb002, "INVMB_K02");

            entity.Property(e => e.Mb001)
                .HasMaxLength(40)
                .HasDefaultValue("")
                .IsFixedLength()
                .HasColumnName("MB001");
            entity.Property(e => e.Company)
                .HasMaxLength(20)
                .HasDefaultValue("")
                .HasColumnName("COMPANY");
            entity.Property(e => e.CreateDate)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("CREATE_DATE");
            entity.Property(e => e.CreateTime)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("CREATE_TIME");
            entity.Property(e => e.Creator)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("CREATOR");
            entity.Property(e => e.DataGroup)
                .HasMaxLength(10)
                .HasDefaultValue("");
            entity.Property(e => e.DataUser)
                .HasMaxLength(10)
                .HasDefaultValue("");
            entity.Property(e => e.Flag)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("FLAG");
            entity.Property(e => e.Mb002)
                .HasMaxLength(60)
                .HasDefaultValue("")
                .HasColumnName("MB002");
            entity.Property(e => e.Mb003)
                .HasMaxLength(60)
                .HasDefaultValue("")
                .HasColumnName("MB003");
            entity.Property(e => e.Mb004)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB004");
            entity.Property(e => e.Mb005)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB005");
            entity.Property(e => e.Mb006)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB006");
            entity.Property(e => e.Mb007)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB007");
            entity.Property(e => e.Mb008)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB008");
            entity.Property(e => e.Mb009)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("MB009");
            entity.Property(e => e.Mb010)
                .HasMaxLength(40)
                .HasDefaultValue("")
                .HasColumnName("MB010");
            entity.Property(e => e.Mb011)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB011");
            entity.Property(e => e.Mb012)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB012");
            entity.Property(e => e.Mb013)
                .HasMaxLength(20)
                .HasDefaultValue("")
                .HasColumnName("MB013");
            entity.Property(e => e.Mb014)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(11, 6)")
                .HasColumnName("MB014");
            entity.Property(e => e.Mb015)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB015");
            entity.Property(e => e.Mb016)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB016");
            entity.Property(e => e.Mb017)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB017");
            entity.Property(e => e.Mb018)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB018");
            entity.Property(e => e.Mb019)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB019");
            entity.Property(e => e.Mb020)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB020");
            entity.Property(e => e.Mb021)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB021");
            entity.Property(e => e.Mb022)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB022");
            entity.Property(e => e.Mb023)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(4, 0)")
                .HasColumnName("MB023");
            entity.Property(e => e.Mb024)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(4, 0)")
                .HasColumnName("MB024");
            entity.Property(e => e.Mb025)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB025");
            entity.Property(e => e.Mb026)
                .HasMaxLength(2)
                .HasDefaultValue("")
                .HasColumnName("MB026");
            entity.Property(e => e.Mb027)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB027");
            entity.Property(e => e.Mb028)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("MB028");
            entity.Property(e => e.Mb029)
                .HasMaxLength(20)
                .HasDefaultValue("")
                .HasColumnName("MB029");
            entity.Property(e => e.Mb030)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("MB030");
            entity.Property(e => e.Mb031)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("MB031");
            entity.Property(e => e.Mb032)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB032");
            entity.Property(e => e.Mb033)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB033");
            entity.Property(e => e.Mb034)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB034");
            entity.Property(e => e.Mb035)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB035");
            entity.Property(e => e.Mb036)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("MB036");
            entity.Property(e => e.Mb037)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("MB037");
            entity.Property(e => e.Mb038)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(11, 3)")
                .HasColumnName("MB038");
            entity.Property(e => e.Mb039)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(11, 3)")
                .HasColumnName("MB039");
            entity.Property(e => e.Mb040)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(11, 3)")
                .HasColumnName("MB040");
            entity.Property(e => e.Mb041)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(11, 3)")
                .HasColumnName("MB041");
            entity.Property(e => e.Mb042)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB042");
            entity.Property(e => e.Mb043)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB043");
            entity.Property(e => e.Mb044)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB044");
            entity.Property(e => e.Mb045)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("MB045");
            entity.Property(e => e.Mb046)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB046");
            entity.Property(e => e.Mb047)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB047");
            entity.Property(e => e.Mb048)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB048");
            entity.Property(e => e.Mb049)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB049");
            entity.Property(e => e.Mb050)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB050");
            entity.Property(e => e.Mb051)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB051");
            entity.Property(e => e.Mb052)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB052");
            entity.Property(e => e.Mb053)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB053");
            entity.Property(e => e.Mb054)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB054");
            entity.Property(e => e.Mb055)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB055");
            entity.Property(e => e.Mb056)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB056");
            entity.Property(e => e.Mb057)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB057");
            entity.Property(e => e.Mb058)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB058");
            entity.Property(e => e.Mb059)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB059");
            entity.Property(e => e.Mb060)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB060");
            entity.Property(e => e.Mb061)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB061");
            entity.Property(e => e.Mb062)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB062");
            entity.Property(e => e.Mb063)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB063");
            entity.Property(e => e.Mb064)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("MB064");
            entity.Property(e => e.Mb065)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("MB065");
            entity.Property(e => e.Mb066)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB066");
            entity.Property(e => e.Mb067)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB067");
            entity.Property(e => e.Mb068)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB068");
            entity.Property(e => e.Mb069)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB069");
            entity.Property(e => e.Mb070)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB070");
            entity.Property(e => e.Mb071)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(8, 3)")
                .HasColumnName("MB071");
            entity.Property(e => e.Mb072)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB072");
            entity.Property(e => e.Mb073)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(8, 3)")
                .HasColumnName("MB073");
            entity.Property(e => e.Mb074)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(8, 3)")
                .HasColumnName("MB074");
            entity.Property(e => e.Mb075)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(8, 3)")
                .HasColumnName("MB075");
            entity.Property(e => e.Mb076)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("MB076");
            entity.Property(e => e.Mb077)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB077");
            entity.Property(e => e.Mb078)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("MB078");
            entity.Property(e => e.Mb079)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("MB079");
            entity.Property(e => e.Mb080)
                .HasMaxLength(20)
                .HasDefaultValue("")
                .HasColumnName("MB080");
            entity.Property(e => e.Mb081)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB081");
            entity.Property(e => e.Mb082)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(7, 4)")
                .HasColumnName("MB082");
            entity.Property(e => e.Mb083)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB083");
            entity.Property(e => e.Mb084)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("MB084");
            entity.Property(e => e.Mb085)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB085");
            entity.Property(e => e.Mb086)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("MB086");
            entity.Property(e => e.Mb087)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB087");
            entity.Property(e => e.Mb088)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(5, 4)")
                .HasColumnName("MB088");
            entity.Property(e => e.Mb089)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(13, 3)")
                .HasColumnName("MB089");
            entity.Property(e => e.Mb090)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB090");
            entity.Property(e => e.Mb091)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB091");
            entity.Property(e => e.Mb092)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB092");
            entity.Property(e => e.Mb093)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(6, 1)")
                .HasColumnName("MB093");
            entity.Property(e => e.Mb094)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(6, 1)")
                .HasColumnName("MB094");
            entity.Property(e => e.Mb095)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(6, 1)")
                .HasColumnName("MB095");
            entity.Property(e => e.Mb096)
                .HasDefaultValue(1m)
                .HasColumnType("numeric(7, 4)")
                .HasColumnName("MB096");
            entity.Property(e => e.Mb097)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB097");
            entity.Property(e => e.Mb098)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB098");
            entity.Property(e => e.Mb099)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(7, 6)")
                .HasColumnName("MB099");
            entity.Property(e => e.Mb100)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB100");
            entity.Property(e => e.Mb101)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB101");
            entity.Property(e => e.Mb102)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB102");
            entity.Property(e => e.Mb103)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB103");
            entity.Property(e => e.Mb104)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB104");
            entity.Property(e => e.Mb105)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB105");
            entity.Property(e => e.Mb106)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB106");
            entity.Property(e => e.Mb107)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB107");
            entity.Property(e => e.Mb108)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB108");
            entity.Property(e => e.Mb109)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB109");
            entity.Property(e => e.Mb110)
                .HasMaxLength(20)
                .HasDefaultValue("")
                .HasColumnName("MB110");
            entity.Property(e => e.Mb111)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB111");
            entity.Property(e => e.Mb112)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB112");
            entity.Property(e => e.Mb113)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB113");
            entity.Property(e => e.Mb114)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB114");
            entity.Property(e => e.Mb115)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB115");
            entity.Property(e => e.Mb116)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB116");
            entity.Property(e => e.Mb117)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB117");
            entity.Property(e => e.Mb118)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB118");
            entity.Property(e => e.Mb119)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(16, 3)")
                .HasColumnName("MB119");
            entity.Property(e => e.Mb120)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(16, 3)")
                .HasColumnName("MB120");
            entity.Property(e => e.Mb121)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB121");
            entity.Property(e => e.Mb122)
                .HasMaxLength(1)
                .HasDefaultValue("0")
                .HasColumnName("MB122");
            entity.Property(e => e.Mb123)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(2, 0)")
                .HasColumnName("MB123");
            entity.Property(e => e.Mb124)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB124");
            entity.Property(e => e.Mb125)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB125");
            entity.Property(e => e.Mb126)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB126");
            entity.Property(e => e.Mb127)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("MB127");
            entity.Property(e => e.Mb128)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB128");
            entity.Property(e => e.Mb129)
                .HasMaxLength(20)
                .HasDefaultValue("0")
                .HasColumnName("MB129");
            entity.Property(e => e.Mb130)
                .HasMaxLength(20)
                .HasDefaultValue("N")
                .HasColumnName("MB130");
            entity.Property(e => e.Mb131)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB131");
            entity.Property(e => e.Mb132)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(13, 2)")
                .HasColumnName("MB132");
            entity.Property(e => e.Mb133)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB133");
            entity.Property(e => e.Mb134)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(11, 3)")
                .HasColumnName("MB134");
            entity.Property(e => e.Mb135)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB135");
            entity.Property(e => e.Mb136)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB136");
            entity.Property(e => e.Mb137)
                .HasMaxLength(6)
                .HasDefaultValue("N")
                .HasColumnName("MB137");
            entity.Property(e => e.Mb138)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB138");
            entity.Property(e => e.Mb139)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB139");
            entity.Property(e => e.Mb140)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB140");
            entity.Property(e => e.Mb141)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB141");
            entity.Property(e => e.Mb142)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB142");
            entity.Property(e => e.Mb143)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB143");
            entity.Property(e => e.Mb144)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB144");
            entity.Property(e => e.Mb145)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB145");
            entity.Property(e => e.Mb146)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB146");
            entity.Property(e => e.Mb147)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB147");
            entity.Property(e => e.Mb148)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB148");
            entity.Property(e => e.Mb149)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB149");
            entity.Property(e => e.Mb150)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB150");
            entity.Property(e => e.Mb151)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB151");
            entity.Property(e => e.Mb152)
                .HasMaxLength(6)
                .HasDefaultValue("")
                .HasColumnName("MB152");
            entity.Property(e => e.Mb153)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB153");
            entity.Property(e => e.Mb154)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB154");
            entity.Property(e => e.Mb155)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB155");
            entity.Property(e => e.Mb156)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB156");
            entity.Property(e => e.Mb157)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB157");
            entity.Property(e => e.Mb158)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("MB158");
            entity.Property(e => e.Mb159)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(16, 3)")
                .HasColumnName("MB159");
            entity.Property(e => e.Mb160)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("MB160");
            entity.Property(e => e.Mb161)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(15, 6)")
                .HasColumnName("MB161");
            entity.Property(e => e.Mb162)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB162");
            entity.Property(e => e.Mb163)
                .HasMaxLength(30)
                .HasDefaultValue("")
                .HasColumnName("MB163");
            entity.Property(e => e.Mb164)
                .HasMaxLength(60)
                .HasDefaultValue("")
                .HasColumnName("MB164");
            entity.Property(e => e.Mb165)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("MB165");
            entity.Property(e => e.Mb166)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(8, 5)")
                .HasColumnName("MB166");
            entity.Property(e => e.Mb167)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("MB167");
            entity.Property(e => e.Mb168)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB168");
            entity.Property(e => e.Mb169)
                .HasMaxLength(1)
                .HasDefaultValue("1")
                .HasColumnName("MB169");
            entity.Property(e => e.Mb170)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB170");
            entity.Property(e => e.Mb171)
                .HasDefaultValue(999999m)
                .HasColumnType("numeric(16, 3)")
                .HasColumnName("MB171");
            entity.Property(e => e.Mb172)
                .HasDefaultValue(5m)
                .HasColumnType("numeric(16, 3)")
                .HasColumnName("MB172");
            entity.Property(e => e.Mb173)
                .HasDefaultValue(10m)
                .HasColumnType("numeric(16, 3)")
                .HasColumnName("MB173");
            entity.Property(e => e.Mb174)
                .HasMaxLength(32)
                .HasDefaultValue("")
                .HasColumnName("MB174");
            entity.Property(e => e.Mb175)
                .HasMaxLength(32)
                .HasDefaultValue("")
                .HasColumnName("MB175");
            entity.Property(e => e.Mb176)
                .HasMaxLength(32)
                .HasDefaultValue("")
                .HasColumnName("MB176");
            entity.Property(e => e.Mb177)
                .HasMaxLength(32)
                .HasDefaultValue("")
                .HasColumnName("MB177");
            entity.Property(e => e.Mb178)
                .HasMaxLength(32)
                .HasDefaultValue("")
                .HasColumnName("MB178");
            entity.Property(e => e.Mb179)
                .HasMaxLength(1)
                .HasDefaultValue("1")
                .HasColumnName("MB179");
            entity.Property(e => e.Mb180)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(4, 0)")
                .HasColumnName("MB180");
            entity.Property(e => e.Mb181)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(4, 0)")
                .HasColumnName("MB181");
            entity.Property(e => e.Mb182)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(4, 0)")
                .HasColumnName("MB182");
            entity.Property(e => e.Mb183)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB183");
            entity.Property(e => e.Mb184)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(4, 0)")
                .HasColumnName("MB184");
            entity.Property(e => e.Mb185)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MB185");
            entity.Property(e => e.Mb186)
                .HasMaxLength(20)
                .HasDefaultValue("")
                .HasColumnName("MB186");
            entity.Property(e => e.Mb187)
                .HasMaxLength(15)
                .HasDefaultValue("")
                .HasColumnName("MB187");
            entity.Property(e => e.Mb188)
                .HasMaxLength(40)
                .HasDefaultValue("")
                .HasColumnName("MB188");
            entity.Property(e => e.Mb189)
                .HasMaxLength(1)
                .HasDefaultValue("1")
                .HasColumnName("MB189");
            entity.Property(e => e.Mb190)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB190");
            entity.Property(e => e.Mb191)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(8, 5)")
                .HasColumnName("MB191");
            entity.Property(e => e.Mb192)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(8, 5)")
                .HasColumnName("MB192");
            entity.Property(e => e.Mb193)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("MB193");
            entity.Property(e => e.Mb194)
                .HasMaxLength(1)
                .HasDefaultValue("1")
                .HasColumnName("MB194");
            entity.Property(e => e.Mb195)
                .HasMaxLength(1)
                .HasDefaultValue("1")
                .HasColumnName("MB195");
            entity.Property(e => e.Mb196)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("MB196");
            entity.Property(e => e.Mb197)
                .HasMaxLength(30)
                .HasDefaultValue("N")
                .HasColumnName("MB197");
            entity.Property(e => e.Mb198)
                .HasMaxLength(1)
                .HasDefaultValue("1")
                .HasColumnName("MB198");
            entity.Property(e => e.Mb199)
                .HasMaxLength(1)
                .HasDefaultValue("N")
                .HasColumnName("MB199");
            entity.Property(e => e.Mb200)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(11, 6)")
                .HasColumnName("MB200");
            entity.Property(e => e.ModiDate)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("MODI_DATE");
            entity.Property(e => e.ModiTime)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("MODI_TIME");
            entity.Property(e => e.Modifier)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("MODIFIER");
            entity.Property(e => e.SyncCount)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(3, 0)")
                .HasColumnName("sync_count");
            entity.Property(e => e.SyncDate)
                .HasMaxLength(8)
                .HasDefaultValue("")
                .HasColumnName("sync_date");
            entity.Property(e => e.SyncMark)
                .HasMaxLength(1)
                .HasDefaultValue("")
                .HasColumnName("sync_mark");
            entity.Property(e => e.SyncTime)
                .HasMaxLength(12)
                .HasDefaultValue("")
                .HasColumnName("sync_time");
            entity.Property(e => e.TransName)
                .HasMaxLength(20)
                .HasDefaultValue("")
                .HasColumnName("TRANS_NAME");
            entity.Property(e => e.TransType)
                .HasMaxLength(4)
                .HasDefaultValue("")
                .HasColumnName("TRANS_TYPE");
            entity.Property(e => e.Udf01)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("UDF01");
            entity.Property(e => e.Udf02)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("UDF02");
            entity.Property(e => e.Udf03)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("UDF03");
            entity.Property(e => e.Udf04)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("UDF04");
            entity.Property(e => e.Udf05)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("UDF05");
            entity.Property(e => e.Udf06)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("UDF06");
            entity.Property(e => e.Udf07)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("UDF07");
            entity.Property(e => e.Udf08)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("UDF08");
            entity.Property(e => e.Udf09)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("UDF09");
            entity.Property(e => e.Udf10)
                .HasDefaultValue(0m)
                .HasColumnType("numeric(21, 6)")
                .HasColumnName("UDF10");
            entity.Property(e => e.UsrGroup)
                .HasMaxLength(10)
                .HasDefaultValue("")
                .HasColumnName("USR_GROUP");
        });

        modelBuilder.Entity<Invmc>(entity =>
        {
            entity.HasKey(e => e.Mc001);

            entity.ToTable("INVMC");

            entity.Property(e => e.Mc001)
                .HasMaxLength(40)
                .HasDefaultValue("")
                .IsFixedLength()
                .HasColumnName("MC001");
            entity.Property(e => e.Mc002)
                .HasMaxLength(60)
                .HasDefaultValue("")
                .HasColumnName("MC002");
        });

        modelBuilder.Entity<Invmh>(entity =>
        {
            // 設置複合主鍵
            entity.HasKey(e => new { e.MH001, e.MH002 });

            entity.ToTable("INVMH");

            entity.Property(e => e.MH001)
                .HasMaxLength(40)
                .HasDefaultValue("")
                .IsFixedLength()
                .HasColumnName("MH001");
            entity.Property(e => e.MH002)
                .HasMaxLength(60)
                .HasDefaultValue("")
                .HasColumnName("MH002");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
