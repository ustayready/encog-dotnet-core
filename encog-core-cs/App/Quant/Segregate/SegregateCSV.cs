﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Encog.Util.CSV;
using System.Collections;
using System.IO;
using Encog.App.Quant.Basic;
using Encog.MathUtil.Randomize;

namespace Encog.App.Quant.Segregate
{
    /// <summary>
    /// This class is used to segregate a CSV file into several sub-files.  This can
    /// be used to create training and evaluation datasets.
    /// </summary>
    public class SegregateCSV : BasicFile
    {
        /// <summary>
        /// The segregation targets.
        /// </summary>
        public IList<SegregateTargetPercent> Targets { get { return this.targets; } }

        /// <summary>
        /// The segregation targets.
        /// </summary>
        private IList<SegregateTargetPercent> targets = new List<SegregateTargetPercent>();

        /// <summary>
        /// Validate that the data is correct.
        /// </summary>
        private void Validate()
        {
            ValidateAnalyzed();

            if (targets.Count < 1)
            {
                throw new QuantError("There are no segregation targets.");
            }

            if (targets.Count < 2)
            {
                throw new QuantError("There must be at least two segregation targets.");
            }

            int total = 0;
            foreach (SegregateTargetPercent p in this.targets)
            {
                total += p.Percent;
            }

            if (total != 100)
            {
                throw new QuantError("Target percents must equal 100.");
            }
        }

        /// <summary>
        /// Balance the targets.
        /// </summary>
        private void BalanceTargets()
        {
            SegregateTargetPercent smallestItem = null;
            int numberAssigned = 0;

            // first try to assign as many as can be assigned
            foreach (SegregateTargetPercent p in this.targets)
            {
                    SegregateTargetPercent stp = (SegregateTargetPercent)p;

                    // assign a number of records to this 
                    double percent = stp.Percent / 100.0;
                    int c = (int)(this.RecordCount * percent);
                    stp.NumberRemaining = c;

                    // track the smallest group
                    if (smallestItem == null || smallestItem.Percent > stp.Percent)
                    {
                        smallestItem = stp;
                    }

                    numberAssigned += c;
                
            }

            // see if there are any remaining items
            int remain = this.RecordCount - numberAssigned;

            // if there are extras, just add them to the largest group
            if (remain > 0)
            {
                smallestItem.NumberRemaining += remain;
            }
        }

        /// <summary>
        /// Analyze the input file.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="format">The format of the input file.</param>
        public void Analyze(String inputFile, bool headers, CSVFormat format)
        {
            this.InputFilename = inputFile;
            this.ExpectInputHeaders = headers;
            this.InputFormat = format;

            this.Analyzed = true;

            PerformBasicCounts();

            BalanceTargets();
        }


        /// <summary>
        /// Process the input file and segregate into the output files.
        /// </summary>
        public void Process()
        {
            Validate();

            ReadCSV csv = new ReadCSV(this.InputFilename, this.ExpectInputHeaders, this.InputFormat);
            ResetStatus();
            foreach (SegregateTargetPercent target in this.targets)
            {
                    TextWriter tw = this.PrepareOutputFile(target.Filename);

                    while (target.NumberRemaining > 0 && csv.Next())
                    {
                        UpdateStatus(false);
                        LoadedRow row = new LoadedRow(csv);
                        WriteRow(tw, row);
                        target.NumberRemaining--;
                    }

                    tw.Close();                
            }
            ReportDone(false);
            csv.Close();
        }
    }
}