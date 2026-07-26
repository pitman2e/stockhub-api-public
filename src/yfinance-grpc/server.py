import os
from concurrent import futures
import grpc
import yfinance as yf

import yfinance_pb2
import yfinance_pb2_grpc


class YFinanceService(yfinance_pb2_grpc.YFinanceServiceServicer):

    def GetHistory(self, request, context):
        ticker = yf.Ticker(request.ticker)
        start_date = request.start_date if request.start_date else '1970-01-01'
        end_date = request.end_date if request.end_date else '1970-01-01'

        hist = ticker.history(start=start_date, end=end_date)
        return yfinance_pb2.CsvResponse(csv_data=hist.to_csv())

    def GetDividends(self, request, context):
        tickers = yf.Ticker(request.ticker)
        dividends = tickers.dividends
        return yfinance_pb2.CsvResponse(csv_data=dividends.to_csv())

    def HealthTest(self, request, context):
        return yfinance_pb2.HealthResponse(status="OK")


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    yfinance_pb2_grpc.add_YFinanceServiceServicer_to_server(YFinanceService(), server)
    port = os.environ.get("PORT", "50051")
    server.add_insecure_port(f'[::]:{port}')
    print(f"gRPC server started on port {port}...")
    server.start()
    server.wait_for_termination()


if __name__ == '__main__':
    serve()
