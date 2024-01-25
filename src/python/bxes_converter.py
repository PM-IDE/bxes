import sys

from ficus.grpc_pipelines.context_values import StringContextValue
from ficus.grpc_pipelines.grpc_pipelines import Pipeline2
from ficus.grpc_pipelines.xes_parts import WriteLogToBxes2, ReadLogFromXes2

Pipeline2(
    ReadLogFromXes2(),
    WriteLogToBxes2(save_path=sys.argv[2])
).execute({
    'path': StringContextValue(sys.argv[1])
})
