NDF format is a txt file format (based on csv) which could be used to export C# memmory data tables to txt file. 
It could be loaded again in future or used to exchange data between different C# programs.

NDF format is a strong type data format, it exports columns' data type to NDF data too and it could be used to initialize data table when it is loaded.

NDF format also contains spec definitions (low & high limits and unit), which is very useful during statistical analysis. 
But current C# data table could not store both data and specs, we need to create one more data table in memory to store specs.

Currently, NDF format is developed for C# only, everyone could help to develop more libraries to support more programming langues.
