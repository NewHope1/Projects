<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.FileSystem.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.dll</Reference>
  <Namespace>System.IO.Compression</Namespace>
</Query>

void Main()
{
	var zipFiles = Directory.GetFiles(@"C:\New_Keisha_IVR\Claims\June", "*.zip");
	//var zipFiles = Directory.GetFiles(@"C:\New folder (26)", "*.zip");
	//var zipFiles = Directory.GetFiles(@"C:\New_Keisha_IVR\Financial\June", "*.zip");
	//var zipFiles = Directory.GetFiles(@"C:\New_Keisha_IVR\PA\June", "*.zip");
	//var zipFiles = Directory.GetFiles(@"C:\New_Keisha_IVR\ProviderEnrollment\June", "*.zip");
	//var zipFiles = Directory.GetFiles(@"C:\New_Keisha_IVR\IVRCard\June", "*.zip");

	//var zipFiles = Directory.GetFiles(@"C:\New folder (19)", "*.zip");


	String searchString = "</Id>"; // Claims & Checks
	//String searchString = "<ns1:Id>AVRS</ns1:Id>"; // PA
	//String searchString = "<GetEnrollmentStatusRequest"; // Provider Enrollment
	//String searchString = "<AddCardRequest"; // Card Requests
	
	
	 // for Provider Enrollment - true
	 // for Claims - false
	 // for Checks (Financial) - false
	 // for PA - true
	 // for Card - true
	Boolean isMatching = false;

	int totalFileCount = 0;
	int matchedTxtCount = 0;
	int zipFileIvrFileCount = 0;
	int totalIvrFileCount = 0;
	//int zipFileNonIvrFileCount = 0;
	String currEntry = String.Empty;

	StreamWriter sw = null;
	
	// initialize current file in archive
	String fileNameOnly = Path.GetFileName(zipFiles[0]);
	currEntry = fileNameOnly.Substring(0, 10);

	// Obtain log file name from zip file based the type of transaction in the zip file
	ZipArchive archive = ZipFile.Open(zipFiles[0], ZipArchiveMode.Read); // get first zip file
	String logFile = LogFileName(archive); // get log file from first zip file
	
	logFile = logFile + currEntry.Substring(6, 4) + currEntry.Substring(0, 2) + currEntry.Substring(3, 2); //append date to log file name
	if (!File.Exists(logFile))
	{
		sw = new StreamWriter(logFile);
	}
	
	foreach (var zipFile in zipFiles)
	{
		fileNameOnly = Path.GetFileName(zipFile); // file name without path
		
		// if new archive
		if (currEntry != fileNameOnly.Substring(0, 10))
		{
			currEntry.Dump();
			zipFileIvrFileCount.Dump("Daily IVR file count: ");
			totalIvrFileCount += zipFileIvrFileCount; //store IVR count
			zipFileIvrFileCount = 0; //reset IVR file count
			currEntry = fileNameOnly.Substring(0, 10); //update current archive name

			// flush and close current log file
			if (sw != null)
			{
				sw.Flush();
				sw.Close();
			}

			//String dtInZipFileName = fileNameOnly.Substring(0, 10);

			// Create new log file, but if log file exists already, don't create one
			ZipArchive currArchive = ZipFile.Open(zipFile, ZipArchiveMode.Read);
			logFile = LogFileName(currArchive); // get log file from first zip file
			logFile = logFile + currEntry.Substring(6, 4) + currEntry.Substring(0, 2) + currEntry.Substring(3, 2); //append date to log file name
			if (!File.Exists(logFile))
			{
				sw = new StreamWriter(logFile);
			}
		}


		// obtain IVR counts and all file counts in the zipFile
		int fileCount = ZipFileCount(zipFile, searchString, out matchedTxtCount, sw);

		if (isMatching)
		{
			zipFileIvrFileCount += matchedTxtCount;
		}
		else
		{
			zipFileIvrFileCount += (fileCount - matchedTxtCount);
		}
	
		totalFileCount += fileCount;
	}

	sw.Flush();
	sw.Close();
	
	totalIvrFileCount += zipFileIvrFileCount; //and capture the count from the last archive
	
	// print last archive counts
	currEntry.Dump();
	zipFileIvrFileCount.Dump("Daily IVR file count: ");
	
	totalFileCount.Dump("Total files in all archives: ", true);
	totalIvrFileCount.Dump("Total IVR files in all archives: ", true);
}

// Get IVR files count within a zip archive and also create a log file if log file is missing
public static int ZipFileCount(String zipFileName, String searchString, out int matchedTxtCount, System.IO.StreamWriter sw)
{
	String recFormat = "'{0:yyyyMMddhhmmssff}','{1}','999999999','0011','000','00','00','00','00','{2}','{3}','{4}','{5}','{6}'";
	  
	matchedTxtCount = 0;	
	using (ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
	{
		int fileCount = 0;

		// We count only named (i.e. that are with files) entries
		int i=0;
		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			if (!String.IsNullOrEmpty(entry.FullName) && !String.Equals(entry.FullName.Substring(entry.FullName.Length - 1), "/")) // don't count folders which have "/" at the end of their FullName
			{
				String fileName = "c:\\temp\\TempIVR" + Convert.ToString(i) + ".xml";
				entry.ExtractToFile(fileName, true);
				
				String field11 = String.Empty;
				String field10 = String.Empty;
				String field2 = String.Empty;
				int checkCnt= 0 ;
				int claimCnt = 0;
				int eligibiltyCnt = 0;
				
				switch (entry.Name.Substring(0, 2))
				{
					case "CS": //Claims
						field10 = "CXH";
						field11 = "X";
						field2 = "C";
						claimCnt = 1;
						break;

					case "PS": //Check
						field10 = "CNH";
						field11 = "X";
						field2 = "P";
						checkCnt = 1;
						break;

					case "PA": //Prior authorization
						field10 = "CXH";
						field11 = "X";
						field2 = "A";
						break;

					case "IC": //Card 
						field10 = "CXH";
						field11 = "X";
						field2 = "R";
						break;

					case "PE": //Provider Enrollment
						field10 = "CXH";
						field11 = "X";
						field2 = "E";
						eligibiltyCnt = 1;
						break;

					case "EV": //Managed Care
						field10 = "CXH";
						field11 = "XX";
						field2 = "M";
						eligibiltyCnt = 1;
						break;
				}

				if (sw != null)
				{
					//construct log record to write to log file
					String logLine = String.Format(recFormat, entry.LastWriteTime.DateTime, field2, field10, field11, checkCnt, claimCnt, eligibiltyCnt);
					
					//write log record to file
					sw.WriteLine(logLine);
				}
				
				if (File.Exists(fileName))
				{
					String fileContents = System.IO.File.ReadAllText(fileName);
					if (fileContents.Contains(searchString))
					{
						matchedTxtCount++;
						System.IO.File.Delete(fileName);
					}
					else
					{
						//entry.FullName.Dump();
					}
				}
	
				fileCount += 1;
			}
			
			i++;
		}
		
		return fileCount;
	}
}

public String LogFileName(ZipArchive archive)
{
	String logFile = String.Empty;
	String txnType = String.Empty;
	
	ZipArchiveEntry entry = archive.Entries[1]; // first entry in the archive is the folder, so we get the 2nd entry
	if (!String.IsNullOrEmpty(entry.FullName) && !String.Equals(entry.FullName.Substring(entry.FullName.Length - 1), "/")) // don't count folders which have "/" at the end of their FullName
	{
		txnType = entry.Name.Substring(0, 2);
	}

	if (txnType == "CS" || txnType == "PS" || txnType == "PE") //Claim, Check, Provider Enrollment
	{
		logFile = ("c:\\temp\\arcw.log.");
	}
	else if (txnType == "EV") //Managed Care
	{
		logFile = ("c:\\temp\\armc.log."); 
	}
	
	return logFile;
}
