To handle a 10 GB CSV file without crashing, it’s better not to load the entire file into memory at once. Instead, read the file line by line in batches. 
Something like SqlBulkCopy or BULK INSERT command will help. It is also a good idea to first put the data into a simple temporary table. Also for the large amount of data it’s a great idea to turn off indexes during this process, after that move the data to the final table and rebuild the indexes.
Inserted 29840 trips.
Found 111 duplicates.
