//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SANNET.DataModel
{
    using System;
    
    public partial class GetTrainingDataset1_Result
    {
        public Nullable<System.DateTime> Date { get; set; }
        public Nullable<int> CompanyId { get; set; }
        public Nullable<decimal> RSIShortNormalized { get; set; }
        public int IsRSIShortOverBought { get; set; }
        public int IsRSIShortOverSold { get; set; }
        public int RSIShortJustCrossedIntoOverBought { get; set; }
        public int RSIShortJustCrossedIntoOverSold { get; set; }
        public Nullable<decimal> RSILongNormalized { get; set; }
        public int IsRSILongOverBought { get; set; }
        public int IsRSILongOverSold { get; set; }
        public int RSILongJustCrossedIntoOverBought { get; set; }
        public int RSILongJustCrossedIntoOverSold { get; set; }
        public int RSIShortJustCrossedOverLong { get; set; }
        public int RSIShortGreaterThanLongForAwhile { get; set; }
        public int RSILongJustCrossedOverShort { get; set; }
        public int RSILongGreaterThanShortForAwhile { get; set; }
        public int CCIShortJustCrossedAboveZero { get; set; }
        public int CCIShortJustCrossedBelowZero { get; set; }
        public int CCILongJustCrossedAboveZero { get; set; }
        public int CCILongJustCrossedBelowZero { get; set; }
        public int SMAShortAboveClose { get; set; }
        public int SMALongAboveClose { get; set; }
        public int SMAShortJustCrossedOverLong { get; set; }
        public int SMAShortGreaterThanLongForAwhile { get; set; }
        public int SMALongJustCrossedOverShort { get; set; }
        public int SMALongGreaterThanShortForAwhile { get; set; }
    }
}
