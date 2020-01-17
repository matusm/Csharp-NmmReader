﻿//****************************************************************************************
//
// This is the main class for reading surface scans!
// Class to handle topographic surface scan data (including all metadata) of the SIOS NMM.
//
// Usage:
// 1.) Create an instance of the NmmFileName class.
// 2.) Create an instance of NmmScanData (this class) with the NmmFileName object as parameter.
//     The constructor opens and consumes all relevant files.
// 3.) Now profiles can be extracted in any order by ExtractProfile(). 
//     The profiles are identified by their numerical index
// 4.) Meta data can be extracted from the MetaData property. 
// 
// 
// Author: Michael Matus, 2019
//
//****************************************************************************************


namespace Bev.IO.NmmReader.scan_mode
{
    public class NmmScanData
    {

        #region Ctor

        public NmmScanData(NmmFileName fileNameObject)
        {
            MetaData = new ScanMetaData();
            MetaData.AddDataFrom(fileNameObject);
            MetaData.AddDataFrom(new NmmInstrumentCharacteristcs());
            // firs read the description file so we can check if requested scan index is valid
            MetaData.AddDataFrom(new NmmDescriptionFileParser(fileNameObject));
            // now perform the scan index checks
            if (MetaData.NumberOfScans > 1)
            {
                int scanIndex = fileNameObject.ScanIndex;
                if (scanIndex == 0)
                {
                    scanIndex = 1;
                }
                if (scanIndex > MetaData.NumberOfScans)
                {
                    scanIndex = MetaData.NumberOfScans;
                }
                fileNameObject.SetScanIndex(scanIndex);
                MetaData.AddDataFrom(fileNameObject);
            }
            // at this stage the scan index of fileNameObject should be ok
            MetaData.AddDataFrom(new NmmIndFileParser(fileNameObject));
            MetaData.AddDataFrom(new NmmEnvironmentData(fileNameObject));
            topographyData = new TopographyData(MetaData);
            nmmDat = new NmmDatFileParser(fileNameObject);
            LoadTopographyData();
            // contrary to similar classes of this library, the dat-files are not closed implicitely
            nmmDat.Close();
        }

        #endregion

        #region Properties

        public ScanMetaData MetaData { get; private set; }

        #endregion

        #region Methods

        // this is the main method: returns the profile for a given symbol and index 
        public double[] ExtractProfile(string columnSymbol, int profileIndex, TopographyProcessType type)
        {
            return ExtractProfile(GetColumnIndexFor(columnSymbol), profileIndex, type);
        }

        // overload in case the column index is known already 
        public double[] ExtractProfile(int columnIndex, int profileIndex, TopographyProcessType type)
        {
            return topographyData.ExtractProfile(columnIndex, profileIndex, type);
        }

        public int GetColumnIndexFor(string columnSymbol)
        {
            for (int i = 0; i < MetaData.NumberOfColumnsInFile; i++)
            {
                if (MetaData.ColumnPredicates[i].IsOf(columnSymbol)) return i;
            }
            return -1;
        }

        public ScanColumnPredicate GetPredicateFor(int columnIndex)
        {
            if (columnIndex < 0) return null;
            if (columnIndex >= MetaData.NumberOfColumnsInFile) return null;
            return MetaData.ColumnPredicates[columnIndex];
        }

        #endregion

        #region Private stuff

        private void LoadTopographyData()
        {
            LoadTopographyDataFwd();
            if (nmmDat.BackwardFilePresent)
            {
                LoadTopographyDataBwd();
            }
        }

        private void LoadTopographyDataFwd()
        {
            double[] dataLine = new double[MetaData.NumberOfColumnsInFile];
            for (int iProfile = 0; iProfile < MetaData.NumberOfProfiles; iProfile++)
            {
                for (int iPoint = 0; iPoint < MetaData.NumberOfDataPoints; iPoint++)
                {
                    if (iPoint < MetaData.ForwardProfileLengths[iProfile])
                    {
                        dataLine = nmmDat.NextForwardDataLine();
                    }
                    int lineIndex = iProfile * MetaData.NumberOfDataPoints + iPoint;
                    topographyData.InsertDataLineAt(dataLine, lineIndex, ScanDirection.Forward);
                }
            }
        }

        private void LoadTopographyDataBwd()
        {
            double[] dataLine = new double[MetaData.NumberOfColumnsInFile];
            // advance file pointer for spurios profiles
            for (int i = 0; i < MetaData.SpuriousDataLines; i++)
            {
                dataLine = nmmDat.NextBackwardDataLine();
                // just discard
            }
            for (int iProfile = 0; iProfile < MetaData.NumberOfProfiles; iProfile++)
            {
                for (int iPoint = 0; iPoint < MetaData.NumberOfDataPoints; iPoint++)
                {
                    if (iPoint < MetaData.BackwardProfileLengths[iProfile])
                    {
                        dataLine = nmmDat.NextBackwardDataLine();
                    }
                    int lineIndex = (iProfile + 1) * MetaData.NumberOfDataPoints - iPoint - 1;
                    topographyData.InsertDataLineAt(dataLine, lineIndex, ScanDirection.Backward);
                }
            }
        }

        // fields
        private readonly NmmDatFileParser nmmDat;
        private readonly TopographyData topographyData;
        #endregion

    }
}
