# Query Copy-pasta for my own reference

SELECT * FROM stock_transaction
where stock_id in ('00001.HK', '00005.HK', '00006.HK', 'GOOGL.US', 'MSFT.US', 'VT.US', 'VWRA.LSE') 
order by stock_id, tx_date

select * from stock_portfolio

select * from stock_transaction 
where stock_id in ('00001.HK', '00005.HK', '00006.HK', 'GOOGL.US', 'MSFT.US', 'VT.US', 'VWRA.LSE') 
order by stock_id, tx_date
